using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class InsertAstTests
{
    [Test]
    public void InsertStatement_StoresConflictTargetAliasAndColumns()
    {
        SqlName table = Name("items", 15);
        SqlName alias = Name("i", 24);
        SqlName id = Name("id", 27);
        SqlName name = Name("name", 31);
        SqlExpression value = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(42, 1));
        SqlUpsertClause upsert = Upsert();
        SqlReturningClause returning = Returning();
        SqlTextSpan span = new SqlTextSpan(0, 35);
        SqlInsertStatement insert = SqlInsertStatement.Create(false, SqlConflictActionKind.Ignore, table, alias, new SqlName[] { id, name }, SqlInsertSourceKind.Values, new SqlExpression[][] { new SqlExpression[] { value } }, null, new SqlUpsertClause[] { upsert }, returning, span);

        Assert.That(insert.IsReplaceKeyword, Is.False);
        Assert.That(insert.HasConflictAction(), Is.True);
        Assert.That(insert.ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
        Assert.That(insert.TableName, Is.SameAs(table));
        Assert.That(insert.HasAlias(), Is.True);
        Assert.That(insert.Alias, Is.SameAs(alias));
        Assert.That(insert.HasColumnNames(), Is.True);
        Assert.That(insert.GetColumnNameCount(), Is.EqualTo(2));
        Assert.That(insert.GetColumnNameAt(0), Is.SameAs(id));
        Assert.That(insert.GetColumnNameAt(1), Is.SameAs(name));
        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Values));
        Assert.That(insert.HasValues(), Is.True);
        Assert.That(insert.GetValueRowCount(), Is.EqualTo(1));
        Assert.That(insert.GetValueRowAt(0)[0], Is.SameAs(value));
        Assert.That(insert.HasSourceQuery(), Is.False);
        Assert.That(insert.HasUpsertClauses(), Is.True);
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(insert.GetUpsertClauseAt(0), Is.SameAs(upsert));
        Assert.That(insert.HasReturning(), Is.True);
        Assert.That(insert.Returning, Is.SameAs(returning));
        Assert.That(insert.Span, Is.SameAs(span));
    }

    [Test]
    public void UpsertClause_StoresConflictTargetAndUpdateAction()
    {
        SqlOrderingTerm target = new SqlOrderingTerm(SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(20, 2)), null, SqlOrderingDirection.None, SqlNullOrdering.None, new SqlTextSpan(20, 2));
        SqlExpression conflictWhere = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(25, 8));
        SqlName column = Name("name", 45);
        SqlExpression value = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(52, 13));
        SqlExpression updateWhere = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(70, 10));
        SqlTextSpan span = new SqlTextSpan(10, 70);
        SqlUpsertClause upsert = new SqlUpsertClause(new SqlOrderingTerm[] { target }, conflictWhere, SqlUpsertActionKind.DoUpdate, new SqlName[] { column }, new SqlExpression[] { value }, updateWhere, span);

        Assert.That(upsert.HasConflictTargets(), Is.True);
        Assert.That(upsert.GetConflictTargetAt(0), Is.SameAs(target));
        Assert.That(upsert.HasConflictWhere(), Is.True);
        Assert.That(upsert.ConflictWhere, Is.SameAs(conflictWhere));
        Assert.That(upsert.ActionKind, Is.EqualTo(SqlUpsertActionKind.DoUpdate));
        Assert.That(upsert.HasUpdateAssignments(), Is.True);
        Assert.That(upsert.GetUpdateColumnAt(0), Is.SameAs(column));
        Assert.That(upsert.GetUpdateExpressionAt(0), Is.SameAs(value));
        Assert.That(upsert.HasUpdateWhere(), Is.True);
        Assert.That(upsert.UpdateWhere, Is.SameAs(updateWhere));
        Assert.That(upsert.Span, Is.SameAs(span));
    }

    [Test]
    public void InsertAndUpsertStatements_RejectNullInputs()
    {
        SqlName table = Name("items", 0);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        SqlOrderingTerm orderingTerm = new SqlOrderingTerm(expression, null, SqlOrderingDirection.None, SqlNullOrdering.None, new SqlTextSpan(0, 1));

        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, null!, null, new SqlName[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], null!));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], SqlInsertSourceKind.Values, null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], SqlInsertSourceKind.Values, new SqlExpression[][] { null! }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], SqlInsertSourceKind.Values, new SqlExpression[][] { new SqlExpression[] { null! } }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], SqlInsertSourceKind.Values, new SqlExpression[0][], null, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlInsertStatement.Create(false, SqlConflictActionKind.None, table, null, new SqlName[0], SqlInsertSourceKind.Values, new SqlExpression[0][], null, new SqlUpsertClause[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(null!, null, SqlUpsertActionKind.DoNothing, new SqlName[0], new SqlExpression[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[] { null! }, null, SqlUpsertActionKind.DoNothing, new SqlName[0], new SqlExpression[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[] { orderingTerm }, null, SqlUpsertActionKind.DoUpdate, null!, new SqlExpression[] { expression }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[] { orderingTerm }, null, SqlUpsertActionKind.DoUpdate, new SqlName[] { null! }, new SqlExpression[] { expression }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[] { orderingTerm }, null, SqlUpsertActionKind.DoUpdate, new SqlName[] { table }, null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[] { orderingTerm }, null, SqlUpsertActionKind.DoUpdate, new SqlName[] { table }, new SqlExpression[] { null! }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentException>(() => new SqlUpsertClause(new SqlOrderingTerm[0], null, SqlUpsertActionKind.DoUpdate, new SqlName[] { table }, new SqlExpression[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlUpsertClause(new SqlOrderingTerm[0], null, SqlUpsertActionKind.DoNothing, new SqlName[0], new SqlExpression[0], null, null!));
    }

    static SqlUpsertClause Upsert()
    {
        return new SqlUpsertClause(new SqlOrderingTerm[0], null, SqlUpsertActionKind.DoNothing, new SqlName[0], new SqlExpression[0], null, new SqlTextSpan(35, 20));
    }

    static SqlReturningClause Returning()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(60, 2));
        SqlResultColumn column = new SqlResultColumn(expression, false, null, null, expression.Span, new SqlToken[0]);
        return new SqlReturningClause(new SqlResultColumn[] { column }, new SqlTextSpan(50, 12));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
