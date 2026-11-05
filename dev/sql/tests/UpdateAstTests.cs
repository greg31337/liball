using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class UpdateAstTests
{
    [Test]
    public void UpdateStatement_StoresConflictTargetAssignmentsWhereAndReturning()
    {
        SqlName table = Name("items", 7);
        SqlName alias = Name("i", 16);
        SqlName indexedBy = Name("idx_items_name", 18);
        SqlName column = Name("name", 22);
        SqlExpression value = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(29, 3));
        SqlTableReference from = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(35, 6), Name("source", 35), null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlExpression where = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(40, 6));
        SqlReturningClause returning = Returning();
        SqlOrderingTerm order = new SqlOrderingTerm(SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(62, 2)), null, SqlOrderingDirection.Desc, SqlNullOrdering.None, new SqlTextSpan(62, 7));
        SqlLimitClause limit = new SqlLimitClause(SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(75, 1)), null, new SqlTextSpan(69, 7));
        SqlTextSpan span = new SqlTextSpan(0, 60);
        SqlUpdateStatement update = SqlUpdateStatement.Create(SqlConflictActionKind.Ignore, table, alias, indexedBy, false, new SqlName[] { column }, new SqlExpression[] { value }, new SqlTableReference[] { from }, where, returning, new SqlOrderingTerm[] { order }, limit, span);

        Assert.That(update.HasConflictAction(), Is.True);
        Assert.That(update.ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
        Assert.That(update.TableName, Is.SameAs(table));
        Assert.That(update.HasAlias(), Is.True);
        Assert.That(update.Alias, Is.SameAs(alias));
        Assert.That(update.HasIndexedBy(), Is.True);
        Assert.That(update.IndexedBy, Is.SameAs(indexedBy));
        Assert.That(update.IsNotIndexed, Is.False);
        Assert.That(update.HasAssignments(), Is.True);
        Assert.That(update.GetAssignmentCount(), Is.EqualTo(1));
        Assert.That(update.GetAssignmentColumnAt(0), Is.SameAs(column));
        Assert.That(update.GetAssignmentExpressionAt(0), Is.SameAs(value));
        Assert.That(update.HasFrom(), Is.True);
        Assert.That(update.GetFromCount(), Is.EqualTo(1));
        Assert.That(update.GetFromAt(0), Is.SameAs(from));
        Assert.That(update.HasWhere(), Is.True);
        Assert.That(update.Where, Is.SameAs(where));
        Assert.That(update.HasReturning(), Is.True);
        Assert.That(update.Returning, Is.SameAs(returning));
        Assert.That(update.HasOrderBy(), Is.True);
        Assert.That(update.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(update.GetOrderByAt(0), Is.SameAs(order));
        Assert.That(update.HasLimit(), Is.True);
        Assert.That(update.Limit, Is.SameAs(limit));
        Assert.That(update.Span, Is.SameAs(span));
    }

    [Test]
    public void UpdateStatement_RejectsNullInputs()
    {
        SqlName table = Name("items", 0);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));

        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, null!, null, new SqlName[0], new SqlExpression[0], null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, null!, new SqlExpression[0], null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[] { null! }, new SqlExpression[] { expression }, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[] { table }, null!, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[] { table }, new SqlExpression[] { null! }, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[] { table }, new SqlExpression[0], null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[0], new SqlExpression[0], null, null, null!));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[0], new SqlExpression[0], null!, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, new SqlName[0], new SqlExpression[0], new SqlTableReference[] { null! }, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, null, false, new SqlName[0], new SqlExpression[0], new SqlTableReference[0], null, null, null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlUpdateStatement.Create(SqlConflictActionKind.None, table, null, null, false, new SqlName[0], new SqlExpression[0], new SqlTableReference[0], null, null, new SqlOrderingTerm[] { null! }, null, new SqlTextSpan(0, 0)));
    }

    static SqlReturningClause Returning()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(50, 2));
        SqlResultColumn column = new SqlResultColumn(expression, false, null, null, expression.Span, new SqlToken[0]);
        return new SqlReturningClause(new SqlResultColumn[] { column }, new SqlTextSpan(40, 12));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
