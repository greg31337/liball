using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LiteralAstTests
{
    [Test]
    public void Literal_StoresKindSourceTextAndSpan()
    {
        SqlTextSpan span = new SqlTextSpan(3, 5);
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.String, "'abc'", span);

        Assert.That(literal.Kind, Is.EqualTo(SqlLiteralKind.String));
        Assert.That(literal.IsKind(SqlLiteralKind.String), Is.True);
        Assert.That(literal.IsKind(SqlLiteralKind.Integer), Is.False);
        Assert.That(literal.SourceText, Is.EqualTo("'abc'"));
        Assert.That(literal.Span, Is.SameAs(span));
        Assert.That(literal.HasValue, Is.False);
        Assert.That(literal.Value, Is.Null);
        Assert.That(literal.HasToken(), Is.False);
        Assert.That(literal.IsNullLiteral(), Is.False);
        Assert.That(literal.IsNumericLiteral(), Is.False);
    }

    [Test]
    public void Literal_StoresParsedValue()
    {
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Integer, "123", new SqlTextSpan(0, 3), 123L);

        Assert.That(literal.HasValue, Is.True);
        Assert.That(literal.Value, Is.EqualTo(123L));
        Assert.That(literal.IsNumericLiteral(), Is.True);
    }

    [Test]
    public void Literal_AllowsNullParsedValueForNullLiteral()
    {
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Null, "NULL", new SqlTextSpan(8, 4), null);

        Assert.That(literal.HasValue, Is.True);
        Assert.That(literal.Value, Is.Null);
        Assert.That(literal.IsNullLiteral(), Is.True);
    }

    [Test]
    public void Literal_StoresOptionalToken()
    {
        SqlToken token = SqlToken.Create(SqlTokenKind.NumericLiteral, new SqlTextSpan(2, 4), "3.14");
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Decimal, "3.14", token.Span, 3.14, token);

        Assert.That(literal.HasToken(), Is.True);
        Assert.That(literal.Token, Is.SameAs(token));
        Assert.That(literal.HasValue, Is.True);
        Assert.That(literal.Value, Is.EqualTo(3.14));
    }

    [Test]
    public void Expression_LiteralPayloadUsesLiteralAstNode()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(0, 1));
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(0, 1), 1L);

        expression.Literal = literal;

        Assert.That(expression.Literal, Is.SameAs(literal));
    }

    [Test]
    public void Literal_RejectsNullAndMismatchedInputs()
    {
        Assert.Throws<ArgumentNullException>(() => SqlLiteral.Create(SqlLiteralKind.String, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlLiteral.Create(SqlLiteralKind.String, "", null!));
        Assert.Throws<ArgumentException>(() => SqlLiteral.Create(SqlLiteralKind.String, "'x'", new SqlTextSpan(0, 1)));
        Assert.Throws<ArgumentNullException>(() => SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(0, 1), 1L, null!));
        Assert.Throws<ArgumentException>(() => SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(0, 1), 1L, SqlToken.Create(SqlTokenKind.NumericLiteral, new SqlTextSpan(2, 1), "1")));
        Assert.Throws<ArgumentException>(() => SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(0, 1), 1L, SqlToken.Create(SqlTokenKind.NumericLiteral, new SqlTextSpan(0, 1), "2")));
    }
}
