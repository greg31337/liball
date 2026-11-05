using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DeleteAstTests
{
    [Test]
    public void DeleteStatement_StoresTargetAliasWhereAndReturning()
    {
        SqlName table = Name("items", 12);
        SqlName alias = Name("i", 21);
        SqlName indexedBy = Name("idx_items_id", 24);
        SqlExpression where = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(29, 6));
        SqlReturningClause returning = Returning();
        SqlOrderingTerm order = new SqlOrderingTerm(SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(60, 2)), null, SqlOrderingDirection.Asc, SqlNullOrdering.None, new SqlTextSpan(60, 2));
        SqlLimitClause limit = new SqlLimitClause(SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(70, 1)), null, new SqlTextSpan(64, 7));
        SqlTextSpan span = new SqlTextSpan(0, 50);
        SqlDeleteStatement delete = SqlDeleteStatement.Create(table, alias, indexedBy, false, where, returning, new SqlOrderingTerm[] { order }, limit, span);

        Assert.That(delete.TableName, Is.SameAs(table));
        Assert.That(delete.HasAlias(), Is.True);
        Assert.That(delete.Alias, Is.SameAs(alias));
        Assert.That(delete.HasIndexedBy(), Is.True);
        Assert.That(delete.IndexedBy, Is.SameAs(indexedBy));
        Assert.That(delete.IsNotIndexed, Is.False);
        Assert.That(delete.HasWhere(), Is.True);
        Assert.That(delete.Where, Is.SameAs(where));
        Assert.That(delete.HasReturning(), Is.True);
        Assert.That(delete.Returning, Is.SameAs(returning));
        Assert.That(delete.HasOrderBy(), Is.True);
        Assert.That(delete.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(delete.GetOrderByAt(0), Is.SameAs(order));
        Assert.That(delete.HasLimit(), Is.True);
        Assert.That(delete.Limit, Is.SameAs(limit));
        Assert.That(delete.Span, Is.SameAs(span));
    }

    [Test]
    public void DeleteStatement_RejectsNullInputs()
    {
        SqlName table = Name("items", 0);

        Assert.Throws<ArgumentNullException>(() => SqlDeleteStatement.Create(null!, null, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlDeleteStatement.Create(table, null, null, null, null!));
        Assert.Throws<ArgumentNullException>(() => SqlDeleteStatement.Create(table, null, null, false, null, null, null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlDeleteStatement.Create(table, null, null, false, null, null, new SqlOrderingTerm[] { null! }, null, new SqlTextSpan(0, 0)));
    }

    static SqlReturningClause Returning()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(40, 2));
        SqlResultColumn column = new SqlResultColumn(expression, false, null, null, expression.Span, new SqlToken[0]);
        return new SqlReturningClause(new SqlResultColumn[] { column }, new SqlTextSpan(30, 12));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
