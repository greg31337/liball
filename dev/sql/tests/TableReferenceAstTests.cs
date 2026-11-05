using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TableReferenceAstTests
{
    [Test]
    public void TableReference_SimpleFactoryCreatesEmptyTaggedNode()
    {
        SqlTextSpan span = new SqlTextSpan(4, 5);
        SqlTableReference table = SqlTableReference.Create(SqlTableReferenceKind.Unknown, span);

        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Unknown));
        Assert.That(table.IsKind(SqlTableReferenceKind.Unknown), Is.True);
        Assert.That(table.Span, Is.SameAs(span));
        Assert.That(table.HasName(), Is.False);
        Assert.That(table.HasQuery(), Is.False);
        Assert.That(table.HasFunctionExpression(), Is.False);
        Assert.That(table.HasInner(), Is.False);
        Assert.That(table.HasJoins(), Is.False);
        Assert.That(table.GetJoinCount(), Is.EqualTo(0));
        Assert.That(table.HasAlias(), Is.False);
        Assert.That(table.HasColumnAliases(), Is.False);
        Assert.That(table.GetColumnAliasCount(), Is.EqualTo(0));
    }

    [Test]
    public void TableReference_StoresNamedTableAliasColumnsAndJoins()
    {
        SqlName tableName = Name("users", 0);
        SqlName alias = Name("u", 6);
        SqlName columnAlias = Name("id", 9);
        SqlTableReference right = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(20, 6), Name("orders", 20), null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlJoin join = new SqlJoin(SqlJoinKind.Left, false, true, right, SqlJoinConstraintKind.On, Expression(30, 8), new SqlName[0], new SqlTextSpan(13, 25));
        SqlTableReference table = SqlTableReference.Create(
            SqlTableReferenceKind.Named,
            new SqlTextSpan(0, 38),
            tableName,
            null,
            null,
            null,
            new SqlJoin[] { join },
            alias,
            new SqlName[] { columnAlias });

        Assert.That(table.IsKind(SqlTableReferenceKind.Named), Is.True);
        Assert.That(table.HasName(), Is.True);
        Assert.That(table.Name, Is.SameAs(tableName));
        Assert.That(table.HasAlias(), Is.True);
        Assert.That(table.Alias, Is.SameAs(alias));
        Assert.That(table.HasColumnAliases(), Is.True);
        Assert.That(table.GetColumnAliasAt(0), Is.SameAs(columnAlias));
        Assert.That(table.HasJoins(), Is.True);
        Assert.That(table.GetJoinAt(0), Is.SameAs(join));
    }

    [Test]
    public void TableReference_StoresSubqueryFunctionAndInnerPayloads()
    {
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(1, 8));
        SqlExpression function = SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(12, 7));
        SqlTableReference inner = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(21, 5), Name("items", 21), null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlTableReference table = SqlTableReference.Create(SqlTableReferenceKind.Parenthesized, new SqlTextSpan(0, 30), null, query, function, inner, new SqlJoin[0], null, new SqlName[0]);

        Assert.That(table.HasQuery(), Is.True);
        Assert.That(table.Query, Is.SameAs(query));
        Assert.That(table.HasFunctionExpression(), Is.True);
        Assert.That(table.FunctionExpression, Is.SameAs(function));
        Assert.That(table.HasInner(), Is.True);
        Assert.That(table.Inner, Is.SameAs(inner));
    }

    [Test]
    public void Join_StoresKindFlagsRightSideAndConstraints()
    {
        SqlTableReference right = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(5, 6), Name("orders", 5), null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlExpression onExpression = Expression(20, 10);
        SqlName usingColumn = Name("id", 35);
        SqlTextSpan span = new SqlTextSpan(0, 40);
        SqlJoin join = new SqlJoin(SqlJoinKind.Left, true, true, right, SqlJoinConstraintKind.Using, onExpression, new SqlName[] { usingColumn }, span);

        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Left));
        Assert.That(join.IsNatural, Is.True);
        Assert.That(join.IsOuter, Is.True);
        Assert.That(join.IsNaturalJoin(), Is.True);
        Assert.That(join.IsCommaJoin(), Is.False);
        Assert.That(join.Right, Is.SameAs(right));
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.Using));
        Assert.That(join.HasOnExpression(), Is.True);
        Assert.That(join.OnExpression, Is.SameAs(onExpression));
        Assert.That(join.HasUsingColumns(), Is.True);
        Assert.That(join.GetUsingColumnAt(0), Is.SameAs(usingColumn));
        Assert.That(join.Span, Is.SameAs(span));
    }

    [Test]
    public void SelectCore_FromUsesTableReferences()
    {
        SqlExpression result = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(7, 1));
        SqlResultColumn resultColumn = new SqlResultColumn(result, false, null, null, result.Span, new SqlToken[0]);
        SqlTableReference table = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(12, 5), Name("items", 12), null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlSelectCore core = SqlSelectCore.Create(false, false, new SqlResultColumn[] { resultColumn }, new SqlTableReference[] { table }, null, new SqlExpression[0], null, new SqlWindowDefinition[0], new SqlTextSpan(0, 17));

        Assert.That(core.HasFrom(), Is.True);
        Assert.That(core.GetFromCount(), Is.EqualTo(1));
        Assert.That(core.GetFromAt(0), Is.SameAs(table));
    }

    [Test]
    public void TableReferenceAst_EnumValuesAreStable()
    {
        Assert.That((int)SqlTableReferenceKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlTableReferenceKind.Named, Is.EqualTo(1));
        Assert.That((int)SqlTableReferenceKind.Join, Is.EqualTo(4));
        Assert.That((int)SqlTableReferenceKind.Values, Is.EqualTo(6));
        Assert.That((int)SqlJoinConstraintKind.None, Is.EqualTo(0));
        Assert.That((int)SqlJoinConstraintKind.On, Is.EqualTo(1));
        Assert.That((int)SqlJoinConstraintKind.Using, Is.EqualTo(2));
    }

    [Test]
    public void TableReferenceAst_RejectsNullInputs()
    {
        SqlTableReference right = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 1));
        Assert.Throws<ArgumentNullException>(() => SqlTableReference.Create(SqlTableReferenceKind.Named, null!));
        Assert.Throws<ArgumentNullException>(() => SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 0), null, null, null, null, null!, null, new SqlName[0]));
        Assert.Throws<ArgumentNullException>(() => SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 0), null, null, null, null, new SqlJoin[] { null! }, null, new SqlName[0]));
        Assert.Throws<ArgumentNullException>(() => SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 0), null, null, null, null, new SqlJoin[0], null, null!));
        Assert.Throws<ArgumentNullException>(() => SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 0), null, null, null, null, new SqlJoin[0], null, new SqlName[] { null! }));
        Assert.Throws<ArgumentNullException>(() => new SqlJoin(SqlJoinKind.Inner, false, false, null!, SqlJoinConstraintKind.None, null, new SqlName[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlJoin(SqlJoinKind.Inner, false, false, right, SqlJoinConstraintKind.None, null, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlJoin(SqlJoinKind.Inner, false, false, right, SqlJoinConstraintKind.None, null, new SqlName[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlJoin(SqlJoinKind.Inner, false, false, right, SqlJoinConstraintKind.None, null, new SqlName[0], null!));
    }

    static SqlExpression Expression(int start, int length)
    {
        return SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(start, length));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
