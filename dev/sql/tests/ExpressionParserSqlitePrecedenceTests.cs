using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserSqlitePrecedenceTests
{
    [Test]
    public void ExpressionParser_SqliteConcatBindsAboveMultiplicative()
    {
        SqlExpression expression = Parse("a || b * c");

        AssertBinary(expression, SqlBinaryOperatorKind.Multiply);
        Assert.That(expression.Left, Is.Not.Null);
        AssertBinary(expression.Left!, SqlBinaryOperatorKind.Concat);
        Assert.That(expression.Right, Is.Not.Null);
        AssertName(expression.Right!, "c");
    }

    [Test]
    public void ExpressionParser_SqliteMultiplicativeRightOperandCanContainConcat()
    {
        SqlExpression expression = Parse("a * b || c");

        AssertBinary(expression, SqlBinaryOperatorKind.Multiply);
        Assert.That(expression.Right, Is.Not.Null);
        AssertBinary(expression.Right!, SqlBinaryOperatorKind.Concat);
    }

    [Test]
    public void ExpressionParser_SqliteAdditiveBindsAboveBitwise()
    {
        SqlExpression expression = Parse("a + b & c");

        AssertBinary(expression, SqlBinaryOperatorKind.BitwiseAnd);
        Assert.That(expression.Left, Is.Not.Null);
        AssertBinary(expression.Left!, SqlBinaryOperatorKind.Add);
    }

    [Test]
    public void ExpressionParser_SqliteBitwiseBindsAboveComparison()
    {
        SqlExpression expression = Parse("a & b = c");

        AssertBinary(expression, SqlBinaryOperatorKind.Equal);
        Assert.That(expression.Left, Is.Not.Null);
        AssertBinary(expression.Left!, SqlBinaryOperatorKind.BitwiseAnd);
    }

    [Test]
    public void ExpressionParser_SqliteNotBindsBelowComparisonAndAboveAnd()
    {
        SqlExpression expression = Parse("not a = b and c");

        AssertBinary(expression, SqlBinaryOperatorKind.And);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.Left.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Not));
        Assert.That(expression.Left.Operand, Is.Not.Null);
        AssertBinary(expression.Left.Operand!, SqlBinaryOperatorKind.Equal);
    }

    [Test]
    public void ExpressionParser_SqliteNotBindsBelowBetween()
    {
        SqlExpression expression = Parse("not a between b and c");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Not));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.Between));
    }

    [Test]
    public void ExpressionParser_SqliteNegatedSpecialOperatorKeepsBarePrecedence()
    {
        SqlExpression expression = Parse("a not between b and c and d");

        AssertBinary(expression, SqlBinaryOperatorKind.And);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Between));
        Assert.That(expression.Left.OperatorText, Is.EqualTo("not between and"));
    }

    [Test]
    public void ExpressionParser_SqliteCollateBindsAboveConcat()
    {
        SqlExpression expression = Parse("a collate nocase || b");

        AssertBinary(expression, SqlBinaryOperatorKind.Concat);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Collate));
        Assert.That(expression.Left.Name, Is.Not.Null);
        Assert.That(expression.Left.Name!.GetText(), Is.EqualTo("nocase"));
    }

    [Test]
    public void ExpressionParser_SqliteUnaryBindsAboveCollate()
    {
        SqlExpression expression = Parse("-a collate nocase");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Collate));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(expression.Operand.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Negative));
    }

    [Test]
    public void ExpressionParser_SqliteEscapeBindsToLikeBeforeComparison()
    {
        SqlExpression expression = Parse("a like b escape c = d");

        AssertBinary(expression, SqlBinaryOperatorKind.Equal);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Like));
        Assert.That(expression.Left.Alternate, Is.Not.Null);
        AssertName(expression.Left.Alternate!, "c");
    }

    [Test]
    public void ExpressionParser_SqliteComparisonAndBetweenAreLeftAssociativeAtSameLevel()
    {
        SqlExpression expression = Parse("a = b between c and d");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Between));
        Assert.That(expression.Operand, Is.Not.Null);
        AssertBinary(expression.Operand!, SqlBinaryOperatorKind.Equal);
    }

    [Test]
    public void ExpressionParser_CollateHelperRejectsInvalidInputs()
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context("a"));
        SqlExpression left = Parse("a");

        Assert.Throws<ArgumentNullException>(() => parser.TryParseCollateExpression(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.TryParseCollateExpression(left, -1));
    }

    static void AssertBinary(SqlExpression expression, SqlBinaryOperatorKind operatorKind)
    {
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(operatorKind));
    }

    static void AssertName(SqlExpression expression, string text)
    {
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.Name, Is.Not.Null);
        Assert.That(expression.Name!.GetText(), Is.EqualTo(text));
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
