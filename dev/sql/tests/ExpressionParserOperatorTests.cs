using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserOperatorTests
{
    [TestCase("+a", SqlUnaryOperatorKind.Positive)]
    [TestCase("-a", SqlUnaryOperatorKind.Negative)]
    [TestCase("~a", SqlUnaryOperatorKind.BitwiseNot)]
    [TestCase("not a", SqlUnaryOperatorKind.Not)]
    public void ExpressionParser_StoresUnaryOperatorKind(string text, SqlUnaryOperatorKind expected)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.UnaryOperator, Is.EqualTo(expected));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.None));
        Assert.That(expression.OperatorToken, Is.Not.Null);
        Assert.That(expression.Operand, Is.Not.Null);
    }

    [TestCase("a + b", SqlBinaryOperatorKind.Add)]
    [TestCase("a - b", SqlBinaryOperatorKind.Subtract)]
    [TestCase("a * b", SqlBinaryOperatorKind.Multiply)]
    [TestCase("a / b", SqlBinaryOperatorKind.Divide)]
    [TestCase("a % b", SqlBinaryOperatorKind.Modulo)]
    [TestCase("a || b", SqlBinaryOperatorKind.Concat)]
    [TestCase("a = b", SqlBinaryOperatorKind.Equal)]
    [TestCase("a == b", SqlBinaryOperatorKind.Equal)]
    [TestCase("a != b", SqlBinaryOperatorKind.NotEqual)]
    [TestCase("a <> b", SqlBinaryOperatorKind.NotEqual)]
    [TestCase("a < b", SqlBinaryOperatorKind.Less)]
    [TestCase("a <= b", SqlBinaryOperatorKind.LessOrEqual)]
    [TestCase("a > b", SqlBinaryOperatorKind.Greater)]
    [TestCase("a >= b", SqlBinaryOperatorKind.GreaterOrEqual)]
    [TestCase("a & b", SqlBinaryOperatorKind.BitwiseAnd)]
    [TestCase("a | b", SqlBinaryOperatorKind.BitwiseOr)]
    [TestCase("a ^ b", SqlBinaryOperatorKind.BitwiseXor)]
    [TestCase("a << b", SqlBinaryOperatorKind.ShiftLeft)]
    [TestCase("a >> b", SqlBinaryOperatorKind.ShiftRight)]
    [TestCase("a and b", SqlBinaryOperatorKind.And)]
    [TestCase("a or b", SqlBinaryOperatorKind.Or)]
    public void ExpressionParser_StoresBinaryOperatorKind(string text, SqlBinaryOperatorKind expected)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(expected));
        Assert.That(expression.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.None));
        Assert.That(expression.OperatorToken, Is.Not.Null);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Right, Is.Not.Null);
    }

    [Test]
    public void ExpressionParser_UsesBitwisePrecedenceBelowAdditive()
    {
        SqlExpression expression = Parse("1 + 2 << 3");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.ShiftLeft));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [Test]
    public void ExpressionParser_LeftAssociatesEqualPrecedenceOperators()
    {
        SqlExpression expression = Parse("1 - 2 - 3");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Subtract));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Left.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Subtract));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [Test]
    public void ExpressionParser_UsesAndPrecedenceAboveOr()
    {
        SqlExpression expression = Parse("a or b and c");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Or));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Right.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.And));
    }

    [Test]
    public void ExpressionParser_OperatorKindHelpersReturnNoneForNonOperators()
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context("a"));
        SqlToken token = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "a");

        Assert.That(parser.GetUnaryOperatorKind(token), Is.EqualTo(SqlUnaryOperatorKind.None));
        Assert.That(parser.GetBinaryOperatorKind(token), Is.EqualTo(SqlBinaryOperatorKind.None));
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
