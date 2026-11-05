using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ConstraintAstTests
{
    [Test]
    public void ColumnConstraint_StoresPayloadFields()
    {
        SqlName name = Name("pk_id", 0);
        SqlExpression expression = Expression(SqlExpressionKind.Literal, 10, 1);
        SqlName collation = Name("nocase", 20);
        SqlForeignKeyReference reference = Reference();
        SqlTextSpan span = new SqlTextSpan(0, 30);
        SqlColumnConstraint constraint = new SqlColumnConstraint(SqlConstraintKind.Generated, name, expression, collation, SqlConflictActionKind.Replace, reference, SqlGeneratedColumnKind.Virtual, true, span);

        Assert.That(constraint.Kind, Is.EqualTo(SqlConstraintKind.Generated));
        Assert.That(constraint.HasName(), Is.True);
        Assert.That(constraint.Name, Is.SameAs(name));
        Assert.That(constraint.HasExpression(), Is.True);
        Assert.That(constraint.Expression, Is.SameAs(expression));
        Assert.That(constraint.HasCollationName(), Is.True);
        Assert.That(constraint.CollationName, Is.SameAs(collation));
        Assert.That(constraint.HasConflictAction(), Is.True);
        Assert.That(constraint.ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(constraint.HasReferences(), Is.True);
        Assert.That(constraint.References, Is.SameAs(reference));
        Assert.That(constraint.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
        Assert.That(constraint.IsAutoincrement, Is.True);
        Assert.That(constraint.IsGenerated(), Is.True);
        Assert.That(constraint.Span, Is.SameAs(span));
    }

    [Test]
    public void TableConstraint_StoresColumnsExpressionsConflictAndReference()
    {
        SqlName name = Name("fk_items", 0);
        SqlName column = Name("parent_id", 12);
        SqlExpression expression = Expression(SqlExpressionKind.Binary, 25, 10);
        SqlForeignKeyReference reference = Reference();
        SqlTableConstraint constraint = new SqlTableConstraint(SqlConstraintKind.ForeignKey, name, new SqlName[] { column }, new SqlExpression[] { expression }, SqlConflictActionKind.Abort, reference, new SqlTextSpan(0, 40));

        Assert.That(constraint.Kind, Is.EqualTo(SqlConstraintKind.ForeignKey));
        Assert.That(constraint.HasName(), Is.True);
        Assert.That(constraint.Name, Is.SameAs(name));
        Assert.That(constraint.HasColumns(), Is.True);
        Assert.That(constraint.GetColumnAt(0), Is.SameAs(column));
        Assert.That(constraint.HasExpressions(), Is.True);
        Assert.That(constraint.GetExpressionAt(0), Is.SameAs(expression));
        Assert.That(constraint.HasConflictAction(), Is.True);
        Assert.That(constraint.ConflictAction, Is.EqualTo(SqlConflictActionKind.Abort));
        Assert.That(constraint.HasReferences(), Is.True);
        Assert.That(constraint.References, Is.SameAs(reference));
    }

    [Test]
    public void ForeignKeyReference_StoresColumnsActionsMatchAndDeferral()
    {
        SqlName tableName = Name("parents", 0);
        SqlName column = Name("id", 10);
        SqlName match = Name("simple", 15);
        SqlTextSpan span = new SqlTextSpan(0, 25);
        SqlForeignKeyReference reference = new SqlForeignKeyReference(tableName, new SqlName[] { column }, "CASCADE", "SET NULL", match, true, true, span);

        Assert.That(reference.TableName, Is.SameAs(tableName));
        Assert.That(reference.HasColumnNames(), Is.True);
        Assert.That(reference.GetColumnNameAt(0), Is.SameAs(column));
        Assert.That(reference.HasOnDeleteAction(), Is.True);
        Assert.That(reference.OnDeleteAction, Is.EqualTo("CASCADE"));
        Assert.That(reference.HasOnUpdateAction(), Is.True);
        Assert.That(reference.OnUpdateAction, Is.EqualTo("SET NULL"));
        Assert.That(reference.HasMatchName(), Is.True);
        Assert.That(reference.MatchName, Is.SameAs(match));
        Assert.That(reference.IsDeferrable, Is.True);
        Assert.That(reference.IsInitiallyDeferred, Is.True);
        Assert.That(reference.Span, Is.SameAs(span));
    }

    [Test]
    public void SchemaDefinitions_UseTypedConstraintArrays()
    {
        SqlColumnConstraint columnConstraint = new SqlColumnConstraint(SqlConstraintKind.NotNull, null, null, null, SqlConflictActionKind.None, null, SqlGeneratedColumnKind.None, false, new SqlTextSpan(0, 8));
        SqlTableConstraint tableConstraint = new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, new SqlName[] { Name("id", 0) }, new SqlExpression[0], SqlConflictActionKind.None, null, new SqlTextSpan(0, 12));
        SqlColumnDefinition column = new SqlColumnDefinition(Name("id", 0), "integer", "INTEGER", new SqlColumnConstraint[] { columnConstraint }, null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(0, 10));
        SqlTableDefinition table = new SqlTableDefinition(false, false, Name("items", 0), new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { tableConstraint }, null, false, false, new SqlTextSpan(0, 20));

        Assert.That(column.GetConstraintAt(0), Is.SameAs(columnConstraint));
        Assert.That(table.GetConstraintAt(0), Is.SameAs(tableConstraint));
    }

    [Test]
    public void ConstraintAst_RejectsNullInputs()
    {
        SqlName name = Name("x", 0);
        SqlExpression expression = Expression(SqlExpressionKind.Name, 0, 1);
        Assert.Throws<ArgumentNullException>(() => new SqlColumnConstraint(SqlConstraintKind.NotNull, null, null, null, SqlConflictActionKind.None, null, SqlGeneratedColumnKind.None, false, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, null!, new SqlExpression[0], SqlConflictActionKind.None, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, new SqlName[] { null! }, new SqlExpression[0], SqlConflictActionKind.None, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], null!, SqlConflictActionKind.None, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], new SqlExpression[] { null! }, SqlConflictActionKind.None, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], new SqlExpression[] { expression }, SqlConflictActionKind.None, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(null!, new SqlName[0], "", "", null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, null!, "", "", null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[] { null! }, "", "", null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], null!, "", null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], "", null!, null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], "", "", null, false, false, null!));
    }

    static SqlForeignKeyReference Reference()
    {
        return new SqlForeignKeyReference(Name("parents", 0), new SqlName[] { Name("id", 8) }, "", "", null, false, false, new SqlTextSpan(0, 10));
    }

    static SqlExpression Expression(SqlExpressionKind kind, int start, int length)
    {
        return SqlExpression.Create(kind, new SqlTextSpan(start, length));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
