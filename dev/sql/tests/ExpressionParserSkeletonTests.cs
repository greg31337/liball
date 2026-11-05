using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserSkeletonTests
{
    [Test]
    public void ExpressionParser_ParsesSingleTokenAsLiteralPrimary()
    {
        SqlParserContext context = Context("123");
        SqlExpressionParser parser = new SqlExpressionParser(context);

        SqlExpression expression = parser.ParseExpression();

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.GetTokenCount(), Is.EqualTo(1));
        Assert.That(expression.GetTokenAt(0).Text, Is.EqualTo("123"));
        Assert.That(expression.Literal, Is.Not.Null);
        Assert.That(expression.Literal!.Kind, Is.EqualTo(SqlLiteralKind.Integer));
        Assert.That(expression.Span.Start, Is.EqualTo(0));
        Assert.That(expression.Span.Length, Is.EqualTo(3));
    }

    [Test]
    public void ExpressionParser_BuildsBinaryTreeUsingPrecedence()
    {
        SqlExpression expression = Parse("1 + 2 * 3");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.OperatorText, Is.EqualTo("+"));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Right.OperatorText, Is.EqualTo("*"));
        Assert.That(expression.Right.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Multiply));
        Assert.That(expression.Span.Start, Is.EqualTo(0));
        Assert.That(expression.Span.Length, Is.EqualTo("1 + 2 * 3".Length));
    }

    [Test]
    public void ExpressionParser_BuildsUnaryExpressionBeforeBinaryExpression()
    {
        SqlExpression expression = Parse("-1 * 2");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.OperatorText, Is.EqualTo("*"));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Multiply));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.Left.OperatorText, Is.EqualTo("-"));
        Assert.That(expression.Left.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Negative));
        Assert.That(expression.Left.Operand, Is.Not.Null);
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [Test]
    public void ExpressionParser_ParentPrecedenceStopsAtLowerOrEqualOperator()
    {
        SqlParserContext context = Context("1 + 2");
        SqlExpressionParser parser = new SqlExpressionParser(context);

        SqlExpression expression = parser.ParseExpressionWithPrecedence(60);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Literal, Is.Not.Null);
        Assert.That(expression.Literal!.SourceText, Is.EqualTo("1"));
        Assert.That(context.Peek().Kind, Is.EqualTo(SqlTokenKind.Plus));
    }

    [Test]
    public void ExpressionParser_RecognizesKeywordPrecedence()
    {
        SqlExpression expression = Parse("not a and b or c");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.OperatorText, Is.EqualTo("or"));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Or));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Left.OperatorText, Is.EqualTo("and"));
        Assert.That(expression.Left.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.And));
        Assert.That(expression.Left.Left, Is.Not.Null);
        Assert.That(expression.Left.Left!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.Left.Left.OperatorText, Is.EqualTo("not"));
        Assert.That(expression.Left.Left.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Not));
    }

    [Test]
    public void ExpressionParser_ReportsDiagnosticAtTerminatorWithoutConsuming()
    {
        SqlParserContext context = Context(")");
        SqlExpressionParser parser = new SqlExpressionParser(context);

        SqlExpression expression = parser.ParseExpression();

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Unknown));
        Assert.That(context.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR001"));
        Assert.That(context.Peek().Kind, Is.EqualTo(SqlTokenKind.CloseParen));
    }

    [Test]
    public void ExpressionParser_PrecedenceHelpersRejectInvalidInputs()
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context("1"));

        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParser(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseExpressionWithPrecedence(-1));
        Assert.Throws<ArgumentNullException>(() => parser.GetUnaryPrecedence(null!));
        Assert.Throws<ArgumentNullException>(() => parser.GetBinaryPrecedence(null!));
        Assert.Throws<ArgumentNullException>(() => parser.GetUnaryOperatorKind(null!));
        Assert.Throws<ArgumentNullException>(() => parser.GetBinaryOperatorKind(null!));
    }

    static SqlExpression Parse(string text)
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context(text));
        return parser.ParseExpression();
    }

    static SqlParserContext Context(string text)
    {
        SqlToken[] tokens = SqlLexer.Tokenize(text);
        return SqlParserContext.Create(tokens);
    }
}
