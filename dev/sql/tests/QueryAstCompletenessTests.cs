using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class QueryAstCompletenessTests
{
    [Test]
    public void QueryAndCte_ConstructorsPreserveTypedRelationshipsAndDefaults()
    {
        SqlTextSpan span = Span();
        SqlName name = Name("cte");
        SqlName column = Name("value");
        SqlSelectCore first = Core();
        SqlSelectCore second = Core();
        SqlOrderingTerm ordering = Ordering();
        SqlLimitClause limit = new SqlLimitClause(Expression(), Expression(), span);
        SqlQuery cteQuery = Query();
        SqlCommonTableExpression cte = new SqlCommonTableExpression(name, new SqlName[] { column }, SqlCteMaterializationKind.NotMaterialized, cteQuery, span);
        SqlQuery query = new SqlQuery(
            new SqlCommonTableExpression[] { cte },
            true,
            new SqlSelectCore[] { first, second },
            new SqlCompoundOperatorKind[] { SqlCompoundOperatorKind.Except },
            new SqlOrderingTerm[] { ordering },
            limit,
            span);

        Assert.That(query.CommonTableExpressions[0], Is.SameAs(cte));
        Assert.That(query.IsRecursive, Is.True);
        Assert.That(query.SelectCores, Is.EqualTo(new SqlSelectCore[] { first, second }));
        Assert.That(query.CompoundOperators, Is.EqualTo(new SqlCompoundOperatorKind[] { SqlCompoundOperatorKind.Except }));
        Assert.That(query.OrderBy[0], Is.SameAs(ordering));
        Assert.That(query.Limit, Is.SameAs(limit));
        Assert.That(query.CompoundParts, Is.Empty);
        Assert.That(query.OffsetFetch, Is.Null);
        Assert.That(query.UnsupportedClauses, Is.Empty);
        Assert.That(query.Span, Is.SameAs(span));
        Assert.That(cte.Name, Is.SameAs(name));
        Assert.That(cte.ColumnNames[0], Is.SameAs(column));
        Assert.That(cte.Materialization, Is.EqualTo(SqlCteMaterializationKind.NotMaterialized));
        Assert.That(cte.Query, Is.SameAs(cteQuery));
        Assert.That(cte.Span, Is.SameAs(span));
        Assert.That(cte.HasColumnNames(), Is.True);
        Assert.That(cte.GetColumnNameCount(), Is.EqualTo(1));
        Assert.That(cte.GetColumnNameAt(0), Is.SameAs(column));
        Assert.That(cte.IsMaterialized(), Is.False);
        Assert.That(cte.IsNotMaterialized(), Is.True);
    }

    [Test]
    public void QueryAndCte_RejectEveryInvalidArrayRelationshipAndRequiredReference()
    {
        SqlTextSpan span = Span();
        SqlSelectCore core = Core();
        SqlName name = Name("x");
        SqlQuery query = Query();
        SqlOrderingTerm ordering = Ordering();

        Assert.Throws<ArgumentNullException>(() => new SqlQuery(null!, false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[] { null! }, false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, null!, new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[] { null! }, new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], null!, new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[] { core }, new SqlCompoundOperatorKind[] { SqlCompoundOperatorKind.Union }, new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], null!, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[] { null! }, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[] { ordering }, null, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(null!, new SqlName[0], SqlCteMaterializationKind.None, query, span));
        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(name, null!, SqlCteMaterializationKind.None, query, span));
        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(name, new SqlName[] { null! }, SqlCteMaterializationKind.None, query, span));
        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(name, new SqlName[0], SqlCteMaterializationKind.None, null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(name, new SqlName[0], SqlCteMaterializationKind.None, query, null!));
    }

    [Test]
    public void SelectCoreAndWindow_ConstructorsPreserveAllTypedRelationships()
    {
        SqlTextSpan span = Span();
        SqlExpression expression = Expression();
        SqlResultColumn resultColumn = new SqlResultColumn(expression, false, null, null, span, new SqlToken[0]);
        SqlTableReference table = Table();
        SqlOrderingTerm ordering = new SqlOrderingTerm(expression, Name("collation"), SqlOrderingDirection.Desc, SqlNullOrdering.First, span);
        SqlWindowFrameBoundary start = new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.Preceding, expression, span);
        SqlWindowFrameBoundary end = new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.Following, expression, span);
        SqlWindowFrame frame = new SqlWindowFrame(SqlWindowFrameKind.Range, start, end, SqlWindowFrameExclusionKind.Ties, span);
        SqlName windowName = Name("window_name");
        SqlName baseName = Name("base_name");
        SqlWindowDefinition window = new SqlWindowDefinition(windowName, baseName, new SqlExpression[] { expression }, new SqlOrderingTerm[] { ordering }, frame, span);
        SqlSelectCore core = new SqlSelectCore(true, true, new SqlResultColumn[] { resultColumn }, new SqlTableReference[] { table }, expression, new SqlExpression[] { expression }, expression, new SqlWindowDefinition[] { window }, span);

        Assert.That(core.IsDistinct, Is.True);
        Assert.That(core.IsAll, Is.True);
        Assert.That(core.ResultColumns[0], Is.SameAs(resultColumn));
        Assert.That(core.From[0], Is.SameAs(table));
        Assert.That(core.Where, Is.SameAs(expression));
        Assert.That(core.GroupBy[0], Is.SameAs(expression));
        Assert.That(core.Having, Is.SameAs(expression));
        Assert.That(core.Windows[0], Is.SameAs(window));
        Assert.That(core.IsValues, Is.False);
        Assert.That(core.Values, Is.Empty);
        Assert.That(core.Top, Is.Null);
        Assert.That(core.UnsupportedClauses, Is.Empty);
        Assert.That(ordering.Expression, Is.SameAs(expression));
        Assert.That(ordering.Collation!.GetText(), Is.EqualTo("collation"));
        Assert.That(ordering.Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(ordering.NullOrdering, Is.EqualTo(SqlNullOrdering.First));
        Assert.That(window.Name, Is.SameAs(windowName));
        Assert.That(window.BaseWindowName, Is.SameAs(baseName));
        Assert.That(window.PartitionBy[0], Is.SameAs(expression));
        Assert.That(window.OrderBy[0], Is.SameAs(ordering));
        Assert.That(window.Frame, Is.SameAs(frame));
        Assert.That(frame.Start, Is.SameAs(start));
        Assert.That(frame.End, Is.SameAs(end));
        Assert.That(frame.Exclusion, Is.EqualTo(SqlWindowFrameExclusionKind.Ties));
        Assert.That(start.Expression, Is.SameAs(expression));
    }

    [Test]
    public void SelectCoreAndWindow_RejectEveryInvalidArrayItemAndRequiredReference()
    {
        SqlTextSpan span = Span();
        SqlExpression expression = Expression();
        SqlResultColumn column = new SqlResultColumn(expression, false, null, null, span, new SqlToken[0]);
        SqlTableReference table = Table();
        SqlOrderingTerm ordering = Ordering();
        SqlWindowDefinition window = new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[0], null, span);
        SqlWindowFrameBoundary boundary = new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, span);

        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, null!, new SqlTableReference[0], null, new SqlExpression[0], null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { null! }, new SqlTableReference[0], null, new SqlExpression[0], null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, null!, null, new SqlExpression[0], null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { null! }, null, new SqlExpression[0], null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { table }, null, null!, null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { table }, null, new SqlExpression[] { null! }, null, new SqlWindowDefinition[0], span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { table }, null, new SqlExpression[0], null, null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { table }, null, new SqlExpression[0], null, new SqlWindowDefinition[] { null! }, span));
        Assert.Throws<ArgumentNullException>(() => new SqlSelectCore(false, false, new SqlResultColumn[] { column }, new SqlTableReference[] { table }, null, new SqlExpression[0], null, new SqlWindowDefinition[] { window }, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlOrderingTerm(null!, null, SqlOrderingDirection.None, SqlNullOrdering.None, span));
        Assert.Throws<ArgumentNullException>(() => new SqlOrderingTerm(expression, null, SqlOrderingDirection.None, SqlNullOrdering.None, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlLimitClause(null!, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlLimitClause(expression, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, null!, new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, new SqlExpression[] { null! }, new SqlOrderingTerm[0], null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, new SqlExpression[0], null!, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[] { null! }, null, span));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[] { ordering }, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowFrame(SqlWindowFrameKind.Rows, null!, null, SqlWindowFrameExclusionKind.None, span));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowFrame(SqlWindowFrameKind.Rows, boundary, null, SqlWindowFrameExclusionKind.None, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, null!));
    }

    [Test]
    public void EmptyQueryAndConstraintCollections_ReportZeroCountsAndBoundaryIndexes()
    {
        SqlQuery query = Query();
        SqlTableConstraint constraint = new SqlTableConstraint(SqlConstraintKind.Check, null, new SqlName[0], new SqlExpression[0], SqlConflictActionKind.None, null, Span());
        SqlForeignKeyReference reference = new SqlForeignKeyReference(Name("parent"), new SqlName[0], "", "", null, false, false, Span());

        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(0));
        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(0));
        Assert.That(query.GetCompoundOperatorCount(), Is.EqualTo(0));
        Assert.That(query.GetOrderByCount(), Is.EqualTo(0));
        Assert.That(constraint.GetColumnCount(), Is.EqualTo(0));
        Assert.That(constraint.GetExpressionCount(), Is.EqualTo(0));
        Assert.That(reference.GetColumnNameCount(), Is.EqualTo(0));
        Assert.Throws<IndexOutOfRangeException>(() => query.GetSelectCoreAt(0));
        Assert.Throws<IndexOutOfRangeException>(() => constraint.GetColumnAt(0));
        Assert.Throws<IndexOutOfRangeException>(() => reference.GetColumnNameAt(0));
    }

    [Test]
    public void Join_RejectsNullSpanAfterValidatingOtherRelationships()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlJoin(SqlJoinKind.Inner, false, false, Table(), SqlJoinConstraintKind.None, null, new SqlName[0], null!));
    }

    [Test]
    public void Join_NaturalKindReportsNaturalWithoutLegacyFlag()
    {
        SqlJoin join = new SqlJoin(SqlJoinKind.Natural, false, false, Table(), SqlJoinConstraintKind.None, null, new SqlName[0], Span());

        Assert.That(join.IsNaturalJoin(), Is.True);
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

    static SqlSelectCore Core()
    {
        return new SqlSelectCore(false, false, new SqlResultColumn[0], new SqlTableReference[0], null, new SqlExpression[0], null, new SqlWindowDefinition[0], Span());
    }

    static SqlQuery Query()
    {
        return new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, Span());
    }

    static SqlTableReference Table()
    {
        return new SqlTableReference(SqlTableReferenceKind.Unknown, Span(), null, null, null, null, new SqlJoin[0], null, new SqlName[0]);
    }
}
