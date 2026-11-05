using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ResultColumnAstTests
{
    [Test]
    public void ResultColumn_StoresExpressionAliasQualifierAndTokens()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        SqlName qualifier = Name("items", 0);
        SqlName alias = Name("value", 10);
        SqlToken token = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 1), "x");
        SqlResultColumn column = new SqlResultColumn(expression, true, qualifier, alias, new SqlTextSpan(0, 15), new SqlToken[] { token });

        Assert.That(column.HasExpression(), Is.True);
        Assert.That(column.Expression, Is.SameAs(expression));
        Assert.That(column.IsWildcard, Is.True);
        Assert.That(column.HasQualifier(), Is.True);
        Assert.That(column.Qualifier, Is.SameAs(qualifier));
        Assert.That(column.HasAlias(), Is.True);
        Assert.That(column.Alias, Is.SameAs(alias));
        Assert.That(column.HasTokens(), Is.True);
        Assert.That(column.GetTokenCount(), Is.EqualTo(1));
        Assert.That(column.GetTokenAt(0), Is.SameAs(token));
    }

    [Test]
    public void ResultColumn_RejectsInvalidInputs()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));

        Assert.Throws<ArgumentNullException>(() => new SqlResultColumn(null, false, null, null, new SqlTextSpan(0, 1), new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlResultColumn(expression, false, null, null, null!, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlResultColumn(expression, false, null, null, new SqlTextSpan(0, 1), null!));
        Assert.Throws<ArgumentNullException>(() => new SqlResultColumn(expression, false, null, null, new SqlTextSpan(0, 1), new SqlToken[] { null! }));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
