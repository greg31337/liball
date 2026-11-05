using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class QueryAstTests
{
    [Test]
    public void SelectCore_StoresClausesAndSpan()
    {
        SqlExpression resultExpression = Expression(SqlExpressionKind.Name, 7, 1);
        SqlResultColumn result = new SqlResultColumn(resultExpression, false, null, null, resultExpression.Span, new SqlToken[0]);
        SqlTableReference from = TableReference("items", 12);
        SqlExpression where = Expression(SqlExpressionKind.Binary, 16, 5);
        SqlExpression group = Expression(SqlExpressionKind.Name, 25, 1);
        SqlExpression having = Expression(SqlExpressionKind.Binary, 33, 5);
        SqlWindowDefinition window = WindowDefinition();
        SqlTextSpan span = new SqlTextSpan(0, 40);
        SqlSelectCore core = SqlSelectCore.Create(
            true,
            false,
            new SqlResultColumn[] { result },
            new SqlTableReference[] { from },
            where,
            new SqlExpression[] { group },
            having,
            new SqlWindowDefinition[] { window },
            span);

        Assert.That(core.IsDistinct, Is.True);
        Assert.That(core.IsAll, Is.False);
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(core.GetResultColumnAt(0), Is.SameAs(result));
        Assert.That(core.HasFrom(), Is.True);
        Assert.That(core.GetFromAt(0), Is.SameAs(from));
        Assert.That(core.HasWhere(), Is.True);
        Assert.That(core.Where, Is.SameAs(where));
        Assert.That(core.HasGroupBy(), Is.True);
        Assert.That(core.GetGroupByAt(0), Is.SameAs(group));
        Assert.That(core.HasHaving(), Is.True);
        Assert.That(core.Having, Is.SameAs(having));
        Assert.That(core.HasWindows(), Is.True);
        Assert.That(core.GetWindowAt(0), Is.SameAs(window));
        Assert.That(core.IsValues, Is.False);
        Assert.That(core.HasValues(), Is.False);
        Assert.That(core.GetValueRowCount(), Is.EqualTo(0));
        Assert.That(core.Span, Is.SameAs(span));
    }

    [Test]
    public void Query_StoresCtesCompoundOrderAndLimit()
    {
        SqlSelectCore first = SelectCore(0);
        SqlSelectCore second = SelectCore(10);
        SqlOrderingTerm order = OrderingTerm();
        SqlLimitClause limit = LimitClause();
        SqlQuery cteQuery = SqlQuery.Create(new SqlSelectCore[] { SelectCore(20) }, new SqlTextSpan(20, 8));
        SqlCommonTableExpression cte = new SqlCommonTableExpression(Name("recent", 0), new SqlName[0], SqlCteMaterializationKind.NotMaterialized, cteQuery, new SqlTextSpan(0, 20));
        SqlTextSpan span = new SqlTextSpan(0, 80);
        SqlQuery query = SqlQuery.Create(
            new SqlCommonTableExpression[] { cte },
            true,
            new SqlSelectCore[] { first, second },
            new SqlCompoundOperatorKind[] { SqlCompoundOperatorKind.UnionAll },
            new SqlOrderingTerm[] { order },
            limit,
            span);

        Assert.That(query.HasCommonTableExpressions(), Is.True);
        Assert.That(query.GetCommonTableExpressionAt(0), Is.SameAs(cte));
        Assert.That(query.IsRecursive, Is.True);
        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetSelectCoreAt(0), Is.SameAs(first));
        Assert.That(query.GetSelectCoreAt(1), Is.SameAs(second));
        Assert.That(query.IsCompound(), Is.True);
        Assert.That(query.GetCompoundOperatorCount(), Is.EqualTo(1));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(query.HasOrderBy(), Is.True);
        Assert.That(query.GetOrderByAt(0), Is.SameAs(order));
        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Limit, Is.SameAs(limit));
        Assert.That(query.Span, Is.SameAs(span));
    }

    [Test]
    public void CommonTableExpression_StoresNameColumnsMaterializationAndQuery()
    {
        SqlName name = Name("items", 0);
        SqlName column = Name("id", 6);
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[] { SelectCore(12) }, new SqlTextSpan(12, 8));
        SqlCommonTableExpression cte = new SqlCommonTableExpression(name, new SqlName[] { column }, SqlCteMaterializationKind.Materialized, query, new SqlTextSpan(0, 20));

        Assert.That(cte.Name, Is.SameAs(name));
        Assert.That(cte.HasColumnNames(), Is.True);
        Assert.That(cte.GetColumnNameCount(), Is.EqualTo(1));
        Assert.That(cte.GetColumnNameAt(0), Is.SameAs(column));
        Assert.That(cte.IsMaterialized(), Is.True);
        Assert.That(cte.IsNotMaterialized(), Is.False);
        Assert.That(cte.Query, Is.SameAs(query));
    }

    [Test]
    public void OrderingAndLimit_StoreExpressionsAndOptions()
    {
        SqlExpression expression = Expression(SqlExpressionKind.Name, 0, 4);
        SqlName collation = Name("nocase", 8);
        SqlOrderingTerm order = new SqlOrderingTerm(expression, collation, SqlOrderingDirection.Desc, SqlNullOrdering.Last, new SqlTextSpan(0, 20));
        SqlExpression limitExpression = Expression(SqlExpressionKind.Literal, 27, 2);
        SqlExpression offsetExpression = Expression(SqlExpressionKind.Literal, 37, 1);
        SqlLimitClause limit = new SqlLimitClause(limitExpression, offsetExpression, new SqlTextSpan(21, 17));

        Assert.That(order.Expression, Is.SameAs(expression));
        Assert.That(order.HasCollation(), Is.True);
        Assert.That(order.Collation, Is.SameAs(collation));
        Assert.That(order.Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(order.NullOrdering, Is.EqualTo(SqlNullOrdering.Last));
        Assert.That(limit.LimitExpression, Is.SameAs(limitExpression));
        Assert.That(limit.HasOffset(), Is.True);
        Assert.That(limit.OffsetExpression, Is.SameAs(offsetExpression));
    }

    [Test]
    public void WindowDefinition_StoresPartitionOrderAndFrame()
    {
        SqlName name = Name("w", 0);
        SqlName baseName = Name("base", 5);
        SqlExpression partition = Expression(SqlExpressionKind.Name, 20, 6);
        SqlOrderingTerm order = OrderingTerm();
        SqlWindowFrameBoundary start = new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.UnboundedPreceding, null, new SqlTextSpan(40, 19));
        SqlWindowFrameBoundary end = new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, new SqlTextSpan(64, 11));
        SqlWindowFrame frame = new SqlWindowFrame(SqlWindowFrameKind.Rows, start, end, SqlWindowFrameExclusionKind.NoOthers, new SqlTextSpan(35, 50));
        SqlWindowDefinition window = new SqlWindowDefinition(name, baseName, new SqlExpression[] { partition }, new SqlOrderingTerm[] { order }, frame, new SqlTextSpan(0, 90));

        Assert.That(window.HasName(), Is.True);
        Assert.That(window.Name, Is.SameAs(name));
        Assert.That(window.HasBaseWindowName(), Is.True);
        Assert.That(window.BaseWindowName, Is.SameAs(baseName));
        Assert.That(window.HasPartitionBy(), Is.True);
        Assert.That(window.GetPartitionByAt(0), Is.SameAs(partition));
        Assert.That(window.HasOrderBy(), Is.True);
        Assert.That(window.GetOrderByAt(0), Is.SameAs(order));
        Assert.That(window.HasFrame(), Is.True);
        Assert.That(window.Frame, Is.SameAs(frame));
        Assert.That(frame.Kind, Is.EqualTo(SqlWindowFrameKind.Rows));
        Assert.That(frame.HasEnd(), Is.True);
        Assert.That(frame.HasExclusion(), Is.True);
        Assert.That(start.HasExpression(), Is.False);
    }

    [Test]
    public void QueryAst_EnumValuesAreStable()
    {
        Assert.That((int)SqlCompoundOperatorKind.None, Is.EqualTo(0));
        Assert.That((int)SqlCompoundOperatorKind.UnionAll, Is.EqualTo(2));
        Assert.That((int)SqlCteMaterializationKind.NotMaterialized, Is.EqualTo(2));
        Assert.That((int)SqlOrderingDirection.Desc, Is.EqualTo(2));
        Assert.That((int)SqlNullOrdering.Last, Is.EqualTo(2));
        Assert.That((int)SqlWindowFrameKind.Groups, Is.EqualTo(3));
        Assert.That((int)SqlWindowFrameBoundaryKind.CurrentRow, Is.EqualTo(3));
        Assert.That((int)SqlWindowFrameExclusionKind.NoOthers, Is.EqualTo(4));
    }

    [Test]
    public void QueryAst_RejectsNullAndMismatchedInputs()
    {
        SqlSelectCore core = SelectCore(0);
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(new SqlResultColumn[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(false, false, new SqlResultColumn[0], null!, null, new SqlExpression[0], null, new SqlWindowDefinition[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(false, false, new SqlResultColumn[0], new SqlTableReference[0], null, null!, null, new SqlWindowDefinition[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(false, false, new SqlResultColumn[0], new SqlTableReference[0], null, new SqlExpression[0], null, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlSelectCore.Create(new SqlResultColumn[0], null!));
        Assert.Throws<ArgumentNullException>(() => SqlQuery.Create(null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlQuery.Create(new SqlSelectCore[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlQuery.Create(new SqlCommonTableExpression[0], false, new SqlSelectCore[] { core }, null!, new SqlOrderingTerm[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentException>(() => SqlQuery.Create(new SqlCommonTableExpression[0], false, new SqlSelectCore[] { core }, new SqlCompoundOperatorKind[] { SqlCompoundOperatorKind.Union }, new SqlOrderingTerm[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlCommonTableExpression(null!, new SqlName[0], SqlCteMaterializationKind.None, SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(0, 0)), new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlOrderingTerm(null!, null, SqlOrderingDirection.None, SqlNullOrdering.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlLimitClause(null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowDefinition(null, null, null!, new SqlOrderingTerm[0], null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowFrame(SqlWindowFrameKind.Rows, null!, null, SqlWindowFrameExclusionKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, null!));
    }

    static SqlSelectCore SelectCore(int start)
    {
        SqlExpression resultExpression = Expression(SqlExpressionKind.Literal, start + 7, 1);
        SqlResultColumn result = new SqlResultColumn(resultExpression, false, null, null, resultExpression.Span, new SqlToken[0]);
        return SqlSelectCore.Create(new SqlResultColumn[] { result }, new SqlTextSpan(start, 8));
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

    static SqlTableReference TableReference(string text, int start)
    {
        SqlName name = Name(text, start);
        return SqlTableReference.Create(SqlTableReferenceKind.Named, name.Span, name, null, null, null, new SqlJoin[0], null, new SqlName[0]);
    }

    static SqlOrderingTerm OrderingTerm()
    {
        return new SqlOrderingTerm(Expression(SqlExpressionKind.Name, 0, 1), null, SqlOrderingDirection.Asc, SqlNullOrdering.None, new SqlTextSpan(0, 1));
    }

    static SqlLimitClause LimitClause()
    {
        return new SqlLimitClause(Expression(SqlExpressionKind.Literal, 0, 1), null, new SqlTextSpan(0, 7));
    }

    static SqlWindowDefinition WindowDefinition()
    {
        return new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[0], null, new SqlTextSpan(0, 0));
    }
}
