using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DmlAstCompletenessTests
{
    [Test]
    public void AssignmentAndConflictTarget_PreserveTypedRelationships()
    {
        SqlName column = Name("value");
        SqlExpression expression = Expression();
        SqlOrderingTerm ordering = Ordering(expression);
        SqlTextSpan span = Span();

        SqlAssignment assignment = new SqlAssignment(new SqlName[] { column }, expression, span);
        SqlConflictTarget target = new SqlConflictTarget(SqlConflictTargetKind.Constraint, new SqlOrderingTerm[] { ordering }, column, expression, span);

        Assert.That(assignment.Columns[0], Is.SameAs(column));
        Assert.That(assignment.Value, Is.SameAs(expression));
        Assert.That(assignment.Span, Is.SameAs(span));
        Assert.That(target.Kind, Is.EqualTo(SqlConflictTargetKind.Constraint));
        Assert.That(target.IndexedColumns[0], Is.SameAs(ordering));
        Assert.That(target.ConstraintName, Is.SameAs(column));
        Assert.That(target.Predicate, Is.SameAs(expression));
        Assert.That(target.Span, Is.SameAs(span));
    }

    [Test]
    public void AssignmentAndConflictTarget_RejectEveryInvalidCollectionAndReference()
    {
        SqlName name = Name("x");
        SqlExpression expression = Expression();
        SqlOrderingTerm ordering = Ordering(expression);

        Assert.Throws<ArgumentNullException>(() => new SqlAssignment(null!, expression, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlAssignment(new SqlName[] { null! }, expression, Span()));
        Assert.Throws<ArgumentException>(() => new SqlAssignment(new SqlName[0], expression, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlAssignment(new SqlName[] { name }, null!, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlAssignment(new SqlName[] { name }, expression, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlConflictTarget(SqlConflictTargetKind.IndexedColumns, null!, null, null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlConflictTarget(SqlConflictTargetKind.IndexedColumns, new SqlOrderingTerm[] { null! }, null, null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlConflictTarget(SqlConflictTargetKind.IndexedColumns, new SqlOrderingTerm[] { ordering }, null, null, null!));
    }

    [Test]
    public void MergeNodes_PreserveEveryTypedRelationship()
    {
        SqlName targetName = Name("target");
        SqlName alias = Name("t");
        SqlName insertColumn = Name("value");
        SqlTableReference source = Table();
        SqlExpression condition = Expression();
        SqlAssignment assignment = new SqlAssignment(new SqlName[] { insertColumn }, condition, Span());
        SqlMergeWhenClause clause = new SqlMergeWhenClause(SqlMergeMatchKind.NotMatchedByTarget, condition, SqlMergeActionKind.Insert, new SqlAssignment[] { assignment }, new SqlName[] { insertColumn }, new SqlExpression[] { condition }, Span());
        SqlReturningClause returning = new SqlReturningClause(new SqlResultColumn[0], Span());
        SqlMergeStatement merge = new SqlMergeStatement(targetName, alias, source, condition, new SqlMergeWhenClause[] { clause }, returning, Span());

        Assert.That(merge.TableName, Is.SameAs(targetName));
        Assert.That(merge.Alias, Is.SameAs(alias));
        Assert.That(merge.Source, Is.SameAs(source));
        Assert.That(merge.Condition, Is.SameAs(condition));
        Assert.That(merge.Clauses[0], Is.SameAs(clause));
        Assert.That(merge.Returning, Is.SameAs(returning));
        Assert.That(clause.MatchKind, Is.EqualTo(SqlMergeMatchKind.NotMatchedByTarget));
        Assert.That(clause.Condition, Is.SameAs(condition));
        Assert.That(clause.ActionKind, Is.EqualTo(SqlMergeActionKind.Insert));
        Assert.That(clause.Assignments[0], Is.SameAs(assignment));
        Assert.That(clause.InsertColumns[0], Is.SameAs(insertColumn));
        Assert.That(clause.InsertValues[0], Is.SameAs(condition));
    }

    [Test]
    public void MergeNodes_RejectEveryInvalidRequiredReferenceArrayAndItem()
    {
        SqlName name = Name("x");
        SqlTableReference source = Table();
        SqlExpression expression = Expression();
        SqlAssignment assignment = new SqlAssignment(new SqlName[] { name }, expression, Span());
        SqlMergeWhenClause clause = new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Delete, new SqlAssignment[0], new SqlName[0], new SqlExpression[0], Span());

        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(null!, null, source, expression, new SqlMergeWhenClause[0], null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(name, null, null!, expression, new SqlMergeWhenClause[0], null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(name, null, source, null!, new SqlMergeWhenClause[0], null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(name, null, source, expression, null!, null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(name, null, source, expression, new SqlMergeWhenClause[] { null! }, null, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeStatement(name, null, source, expression, new SqlMergeWhenClause[] { clause }, null, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, null!, new SqlName[0], new SqlExpression[0], Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { null! }, new SqlName[0], new SqlExpression[0], Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { assignment }, null!, new SqlExpression[0], Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { assignment }, new SqlName[] { null! }, new SqlExpression[0], Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { assignment }, new SqlName[] { name }, null!, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { assignment }, new SqlName[] { name }, new SqlExpression[] { null! }, Span()));
        Assert.Throws<ArgumentNullException>(() => new SqlMergeWhenClause(SqlMergeMatchKind.Matched, null, SqlMergeActionKind.Update, new SqlAssignment[] { assignment }, new SqlName[] { name }, new SqlExpression[] { expression }, null!));
    }

    [Test]
    public void DmlFactories_InitializeEveryProgressiveOverload()
    {
        SqlName name = Name("items");
        SqlExpression expression = Expression();
        SqlName[] columns = new SqlName[] { name };
        SqlExpression[] expressions = new SqlExpression[] { expression };
        SqlExpression[][] values = new SqlExpression[][] { expressions };
        SqlTableReference[] from = new SqlTableReference[] { Table() };

        SqlInsertStatement basicInsert = SqlInsertStatement.Create(false, SqlConflictActionKind.None, name, null, columns, Span());
        SqlInsertStatement valuesInsert = SqlInsertStatement.Create(false, SqlConflictActionKind.None, name, null, columns, SqlInsertSourceKind.Values, values, null, Span());
        SqlInsertStatement upsertInsert = SqlInsertStatement.Create(false, SqlConflictActionKind.None, name, null, columns, SqlInsertSourceKind.Values, values, null, new SqlUpsertClause[0], Span());
        SqlUpdateStatement basicUpdate = SqlUpdateStatement.Create(SqlConflictActionKind.None, name, null, columns, expressions, null, null, Span());
        SqlUpdateStatement fromUpdate = SqlUpdateStatement.Create(SqlConflictActionKind.None, name, null, columns, expressions, from, null, null, Span());
        SqlUpdateStatement indexedUpdate = SqlUpdateStatement.Create(SqlConflictActionKind.None, name, null, name, true, columns, expressions, from, null, null, Span());
        SqlDeleteStatement basicDelete = SqlDeleteStatement.Create(name, null, null, null, Span());
        SqlDeleteStatement indexedDelete = SqlDeleteStatement.Create(name, null, name, true, null, null, Span());

        Assert.That(basicInsert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Unknown));
        Assert.That(valuesInsert.Values, Is.SameAs(values));
        Assert.That(upsertInsert.UpsertClauses, Is.Empty);
        Assert.That(basicUpdate.From, Is.Empty);
        Assert.That(fromUpdate.From, Is.SameAs(from));
        Assert.That(indexedUpdate.IndexedBy, Is.SameAs(name));
        Assert.That(indexedUpdate.IsNotIndexed, Is.True);
        Assert.That(basicDelete.IndexedBy, Is.Null);
        Assert.That(indexedDelete.IndexedBy, Is.SameAs(name));
        Assert.That(indexedDelete.IsNotIndexed, Is.True);
    }

    [Test]
    public void UpdateAndUpsert_CreateTypedAssignmentsThatMirrorLegacyArrays()
    {
        SqlName column = Name("value");
        SqlExpression value = Expression();
        SqlUpdateStatement update = new SqlUpdateStatement(SqlConflictActionKind.None, Name("items"), null, null, false, new SqlName[] { column }, new SqlExpression[] { value }, new SqlTableReference[0], null, null, new SqlOrderingTerm[0], null, Span());
        SqlUpsertClause upsert = new SqlUpsertClause(new SqlOrderingTerm[0], null, SqlUpsertActionKind.DoUpdate, new SqlName[] { column }, new SqlExpression[] { value }, null, Span());

        Assert.That(update.Assignments, Has.Length.EqualTo(1));
        Assert.That(update.Assignments[0].Columns[0], Is.SameAs(column));
        Assert.That(update.Assignments[0].Value, Is.SameAs(value));
        Assert.That(upsert.Assignments, Has.Length.EqualTo(1));
        Assert.That(upsert.Assignments[0].Columns[0], Is.SameAs(column));
        Assert.That(upsert.Assignments[0].Value, Is.SameAs(value));
        Assert.That(upsert.SyntaxKind, Is.EqualTo(SqlUpsertSyntaxKind.OnConflict));
        Assert.That(upsert.ConflictTarget, Is.Null);
    }

    static SqlTextSpan Span()
    {
        return new SqlTextSpan(0, 0);
    }

    static SqlExpression Expression()
    {
        return new SqlExpression(SqlExpressionKind.Unknown, Span(), new SqlToken[0], new SqlExpression[0]);
    }

    static SqlName Name(string text)
    {
        SqlTextSpan span = new SqlTextSpan(0, text.Length);
        return new SqlName(new SqlNamePart[] { new SqlNamePart(text, text, span, SqlNameQuoteStyle.None) }, span);
    }

    static SqlOrderingTerm Ordering(SqlExpression expression)
    {
        return new SqlOrderingTerm(expression, null, SqlOrderingDirection.None, SqlNullOrdering.None, Span());
    }

    static SqlTableReference Table()
    {
        return new SqlTableReference(SqlTableReferenceKind.Unknown, Span(), null, null, null, null, new SqlJoin[0], null, new SqlName[0]);
    }
}
