using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionAstTests
{
    [Test]
    public void Expression_StoresKindSpanAndEmptyCollections()
    {
        SqlTextSpan span = new SqlTextSpan(2, 5);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, span);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.IsKind(SqlExpressionKind.Name), Is.True);
        Assert.That(expression.IsKind(SqlExpressionKind.Literal), Is.False);
        Assert.That(expression.Span, Is.SameAs(span));
        Assert.That(expression.HasTokens(), Is.False);
        Assert.That(expression.GetTokenCount(), Is.EqualTo(0));
        Assert.That(expression.HasArguments(), Is.False);
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(0));
        Assert.That(expression.Left, Is.Null);
        Assert.That(expression.Right, Is.Null);
        Assert.That(expression.Operand, Is.Null);
        Assert.That(expression.Alternate, Is.Null);
        Assert.That(expression.Name, Is.Null);
        Assert.That(expression.Literal, Is.Null);
        Assert.That(expression.Query, Is.Null);
        Assert.That(expression.TypeName, Is.Null);
        Assert.That(expression.Value, Is.Null);
        Assert.That(expression.OperatorText, Is.Null);
        Assert.That(expression.OperatorToken, Is.Null);
        Assert.That(expression.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.None));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.None));
        Assert.That(expression.IsDistinct, Is.False);
        Assert.That(expression.OrderBy.Length, Is.EqualTo(0));
        Assert.That(expression.Filter, Is.Null);
        Assert.That(expression.WindowDefinition, Is.Null);
    }

    [Test]
    public void Expression_StoresTokenPayload()
    {
        SqlToken token = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 5), "total");
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 5), new SqlToken[] { token });

        Assert.That(expression.HasTokens(), Is.True);
        Assert.That(expression.GetTokenCount(), Is.EqualTo(1));
        Assert.That(expression.GetTokenAt(0), Is.SameAs(token));
        Assert.That(expression.HasArguments(), Is.False);
    }

    [Test]
    public void Expression_StoresArgumentPayload()
    {
        SqlExpression first = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(4, 1));
        SqlExpression second = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(7, 1));
        SqlToken functionName = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 3), "max");
        SqlExpression expression = SqlExpression.Create(
            SqlExpressionKind.FunctionCall,
            new SqlTextSpan(0, 8),
            new SqlToken[] { functionName },
            new SqlExpression[] { first, second });

        Assert.That(expression.HasTokens(), Is.True);
        Assert.That(expression.HasArguments(), Is.True);
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(2));
        Assert.That(expression.GetArgumentAt(0), Is.SameAs(first));
        Assert.That(expression.GetArgumentAt(1), Is.SameAs(second));
    }

    [Test]
    public void Expression_AllowsTaggedPayloadFields()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(0, 5));
        SqlExpression left = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        SqlExpression right = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(4, 1));
        SqlExpression operand = SqlExpression.Create(SqlExpressionKind.Parenthesized, new SqlTextSpan(0, 5));
        SqlExpression alternate = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(6, 1));
        SqlName name = new SqlName(new SqlNamePart[0], new SqlTextSpan(0, 0));
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(0, 1), 1L);
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(0, 0));
        string typeName = "decimal(10, 2)";
        string value = ":parameter";
        SqlToken operatorToken = SqlToken.Create(SqlTokenKind.Plus, new SqlTextSpan(2, 1), "+");
        SqlOrderingTerm order = new SqlOrderingTerm(right, null, SqlOrderingDirection.Desc, SqlNullOrdering.Last, new SqlTextSpan(4, 1));
        SqlWindowDefinition window = new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[0], null, new SqlTextSpan(0, 0));

        expression.Left = left;
        expression.Right = right;
        expression.Operand = operand;
        expression.Alternate = alternate;
        expression.Name = name;
        expression.Literal = literal;
        expression.Query = query;
        expression.TypeName = typeName;
        expression.Value = value;
        expression.OperatorText = "+";
        expression.OperatorToken = operatorToken;
        expression.UnaryOperator = SqlUnaryOperatorKind.Negative;
        expression.BinaryOperator = SqlBinaryOperatorKind.Add;
        expression.IsDistinct = true;
        expression.OrderBy = new SqlOrderingTerm[] { order };
        expression.Filter = right;
        expression.WindowDefinition = window;

        Assert.That(expression.Left, Is.SameAs(left));
        Assert.That(expression.Right, Is.SameAs(right));
        Assert.That(expression.Operand, Is.SameAs(operand));
        Assert.That(expression.Alternate, Is.SameAs(alternate));
        Assert.That(expression.Name, Is.SameAs(name));
        Assert.That(expression.Literal, Is.SameAs(literal));
        Assert.That(expression.Query, Is.SameAs(query));
        Assert.That(expression.TypeName, Is.EqualTo(typeName));
        Assert.That(expression.Value, Is.EqualTo(value));
        Assert.That(expression.OperatorText, Is.EqualTo("+"));
        Assert.That(expression.OperatorToken, Is.SameAs(operatorToken));
        Assert.That(expression.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.Negative));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.IsDistinct, Is.True);
        Assert.That(expression.OrderBy[0], Is.SameAs(order));
        Assert.That(expression.Filter, Is.SameAs(right));
        Assert.That(expression.WindowDefinition, Is.SameAs(window));
    }

    [Test]
    public void Expression_RejectsNullSpanTokensAndArguments()
    {
        Assert.Throws<ArgumentNullException>(() => SqlExpression.Create(SqlExpressionKind.Name, null!));
        Assert.Throws<ArgumentNullException>(() => SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 0), null!));
        Assert.Throws<ArgumentNullException>(() => SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 0), new SqlToken[] { null! }));
        Assert.Throws<ArgumentNullException>(() => SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(0, 0), new SqlToken[0], null!));
        Assert.Throws<ArgumentNullException>(() => SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(0, 0), new SqlToken[0], new SqlExpression[] { null! }));
    }
}
