using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DdlAstCompletenessTests
{
    [Test]
    public void DdlPayloads_PreserveEveryTypedRelationship()
    {
        SqlTextSpan span = Span();
        SqlName matchName = Name("simple");
        SqlToken token = new SqlToken(SqlTokenKind.Identifier, span, "");

        SqlColumnTypeName typeName = new SqlColumnTypeName("decimal(10,2)", "DECIMAL(10, 2)", span);
        SqlForeignKeyClause clause = new SqlForeignKeyClause(SqlForeignKeyClauseKind.OnDelete, SqlForeignKeyActionKind.Cascade, matchName, SqlForeignKeyDeferrabilityKind.Deferrable, SqlForeignKeyInitialTimingKind.Deferred, span);
        SqlVirtualTableArgument argument = new SqlVirtualTableArgument("", new SqlToken[] { token }, span);

        Assert.That(typeName.NormalizedText, Is.EqualTo("decimal(10,2)"));
        Assert.That(typeName.SourceText, Is.EqualTo("DECIMAL(10, 2)"));
        Assert.That(typeName.Span, Is.SameAs(span));
        Assert.That(clause.Kind, Is.EqualTo(SqlForeignKeyClauseKind.OnDelete));
        Assert.That(clause.Action, Is.EqualTo(SqlForeignKeyActionKind.Cascade));
        Assert.That(clause.MatchName, Is.SameAs(matchName));
        Assert.That(clause.Deferrability, Is.EqualTo(SqlForeignKeyDeferrabilityKind.Deferrable));
        Assert.That(clause.InitialTiming, Is.EqualTo(SqlForeignKeyInitialTimingKind.Deferred));
        Assert.That(clause.Span, Is.SameAs(span));
        Assert.That(argument.SourceText, Is.Empty);
        Assert.That(argument.Tokens[0], Is.SameAs(token));
        Assert.That(argument.Span, Is.SameAs(span));
    }

    [Test]
    public void DdlPayloads_RejectEveryRequiredNullAndArrayItem()
    {
        SqlTextSpan span = Span();

        Assert.Throws<ArgumentNullException>(() => new SqlColumnTypeName(null!, "", span));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnTypeName("", null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnTypeName("", "", null!));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyClause(SqlForeignKeyClauseKind.Match, SqlForeignKeyActionKind.None, null, SqlForeignKeyDeferrabilityKind.None, SqlForeignKeyInitialTimingKind.None, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableArgument(null!, new SqlToken[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableArgument("", null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableArgument("", new SqlToken[] { null! }, span));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableArgument("", new SqlToken[0], null!));
    }

    [Test]
    public void SchemaCollections_RejectNullItemsAndSpansAfterValidRelationships()
    {
        SqlTextSpan span = Span();
        SqlName name = Name("x");
        SqlColumnDefinition column = Column();
        SqlTableConstraint constraint = Constraint();
        SqlQuery query = Query();
        SqlOrderingTerm ordering = Ordering();
        SqlToken token = new SqlToken(SqlTokenKind.Identifier, span, "");
        SqlStatement statement = new SqlStatement(SqlStatementKind.Empty, span, new SqlToken[0]);

        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, new SqlColumnDefinition[] { column }, null!, null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { null! }, null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { constraint }, null, false, false, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(name, "", "", new SqlColumnConstraint[] { null! }, null, null, SqlGeneratedColumnKind.None, span));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(name, "", "", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlIndexDefinition(false, false, name, name, new SqlOrderingTerm[] { null! }, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlIndexDefinition(false, false, name, name, new SqlOrderingTerm[] { ordering }, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlViewDefinition(false, false, name, new SqlName[] { null! }, query, span));
        Assert.Throws<ArgumentNullException>(() => new SqlViewDefinition(false, false, name, new SqlName[0], query, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableDefinition(false, name, name, new SqlToken[] { null! }, span));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableDefinition(false, name, name, new SqlToken[] { token }, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Before, SqlTriggerEvent.Update, new SqlName[] { null! }, name, false, null, new SqlStatement[] { statement }, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Before, SqlTriggerEvent.Update, new SqlName[] { name }, name, false, null, new SqlStatement[] { null! }, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Before, SqlTriggerEvent.Update, new SqlName[] { name }, name, false, null, new SqlStatement[] { statement }, null!));
    }

    [Test]
    public void ConstraintConstructors_RejectEveryInvalidCollectionReferenceAndSpan()
    {
        SqlName name = Name("x");
        SqlExpression expression = Expression();
        SqlTextSpan span = Span();

        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, null!, new SqlExpression[0], SqlConflictActionKind.None, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, new SqlName[] { null! }, new SqlExpression[0], SqlConflictActionKind.None, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], null!, SqlConflictActionKind.None, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], new SqlExpression[] { null! }, SqlConflictActionKind.None, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], new SqlExpression[] { expression }, SqlConflictActionKind.None, null, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(null!, new SqlName[0], "", "", null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, null!, "", "", null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[] { null! }, "", "", null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], null!, "", null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], "", null!, null, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlForeignKeyReference(name, new SqlName[0], "", "", null, false, false, null!));
    }

    [Test]
    public void ForeignKeyLegacyActions_MapToEveryTypedAction()
    {
        AssertAction("NO ACTION", SqlForeignKeyActionKind.NoAction);
        AssertAction("RESTRICT", SqlForeignKeyActionKind.Restrict);
        AssertAction("SET NULL", SqlForeignKeyActionKind.SetNull);
        AssertAction("SET DEFAULT", SqlForeignKeyActionKind.SetDefault);
        AssertAction("CASCADE", SqlForeignKeyActionKind.Cascade);
        AssertAction("", SqlForeignKeyActionKind.None);
    }

    static void AssertAction(string text, SqlForeignKeyActionKind expected)
    {
        SqlForeignKeyReference reference = new SqlForeignKeyReference(Name("parent"), new SqlName[0], text, text, null, false, false, Span());

        Assert.That(reference.OnDelete, Is.EqualTo(expected));
        Assert.That(reference.OnUpdate, Is.EqualTo(expected));
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

    static SqlOrderingTerm Ordering()
    {
        return new SqlOrderingTerm(Expression(), null, SqlOrderingDirection.None, SqlNullOrdering.None, Span());
    }

    static SqlColumnDefinition Column()
    {
        return new SqlColumnDefinition(Name("column"), "", "", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, Span());
    }

    static SqlTableConstraint Constraint()
    {
        return new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, new SqlName[0], new SqlExpression[0], SqlConflictActionKind.None, null, Span());
    }

    static SqlQuery Query()
    {
        return new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, Span());
    }
}
