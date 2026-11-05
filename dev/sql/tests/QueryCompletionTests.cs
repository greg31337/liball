using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class QueryCompletionTests
{
    [Test]
    public void TableSources_ExposeFunctionValuesSubqueryAndLateralData()
    {
        SqlQuery sqlite = Parse("select * from json_each(payload) as j, (values (1), (2)) as v(id)", SqlDialectKind.SQLite);
        SqlTableReference function = sqlite.SelectCores[0].From[0];
        SqlTableReference values = sqlite.SelectCores[0].From[1];
        SqlQuery postgres = Parse("select * from users, lateral json_each(users.payload) j", SqlDialectKind.PostgreSql);
        SqlTableReference lateral = postgres.SelectCores[0].From[1];

        Assert.That(function.Kind, Is.EqualTo(SqlTableReferenceKind.Function));
        Assert.That(function.SourceData, Is.Not.Null);
        Assert.That(function.SourceData!.Name!.GetText(), Is.EqualTo("json_each"));
        Assert.That(function.SourceData.FunctionArguments.Length, Is.EqualTo(1));
        Assert.That(values.Kind, Is.EqualTo(SqlTableReferenceKind.Values));
        Assert.That(values.SourceData!.Values.Length, Is.EqualTo(2));
        Assert.That(values.SourceData.Values[1][0].Literal!.SourceText, Is.EqualTo("2"));
        Assert.That(values.SourceData.Query, Is.SameAs(values.Query));
        Assert.That(lateral.SourceData!.IsLateral, Is.True);
        Assert.That(lateral.SourceData.FunctionArguments.Length, Is.EqualTo(1));
    }

    [Test]
    public void Joins_ExposeWrittenModifiersKindsAndConstraints()
    {
        SqlJoin natural = Parse("select * from a natural left outer join b using (id)", SqlDialectKind.SQLite).SelectCores[0].From[0].Joins[0];
        SqlJoin semi = Parse("select * from b left semi join c on b.id = c.id", SqlDialectKind.SQLite).SelectCores[0].From[0].Joins[0];
        SqlJoin apply = Parse("select * from a cross apply f(a.id)", SqlDialectKind.SQLite).SelectCores[0].From[0].Joins[0];
        SqlJoin straight = Parse("select * from a straight_join d", SqlDialectKind.SQLite).SelectCores[0].From[0].Joins[0];

        Assert.That(natural.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.Natural, SqlJoinModifierKind.Left, SqlJoinModifierKind.Outer }));
        Assert.That(natural.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.Using));
        Assert.That(natural.UsingColumns[0].GetText(), Is.EqualTo("id"));
        Assert.That(semi.Kind, Is.EqualTo(SqlJoinKind.Semi));
        Assert.That(semi.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.Left, SqlJoinModifierKind.Semi }));
        Assert.That(semi.Right.Kind, Is.EqualTo(SqlTableReferenceKind.Named));
        Assert.That(semi.Right.Name!.GetText(), Is.EqualTo("c"));
        Assert.That(semi.Right.Alias, Is.Null);
        Assert.That(semi.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.On));
        Assert.That(apply.Kind, Is.EqualTo(SqlJoinKind.CrossApply));
        Assert.That(apply.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.Cross, SqlJoinModifierKind.Apply }));
        Assert.That(straight.Kind, Is.EqualTo(SqlJoinKind.Straight));
        Assert.That(SqlGenerator.GenerateQuery(Parse("select * from a left semi join b", SqlDialectKind.Generic)), Is.EqualTo("SELECT * FROM a LEFT SEMI JOIN b"));
    }

    [Test]
    public void DialectJoinForms_ExposeAntiOuterApplyAndAsOfKinds()
    {
        SqlJoin anti = Parse("select * from a anti join b", SqlDialectKind.Generic).SelectCores[0].From[0].Joins[0];
        SqlJoin apply = Parse("select * from a outer apply f(a.id)", SqlDialectKind.SqlServer).SelectCores[0].From[0].Joins[0];
        SqlJoin asOf = Parse("select * from a asof join b on a.id = b.id", SqlDialectKind.Generic).SelectCores[0].From[0].Joins[0];

        Assert.That(anti.Kind, Is.EqualTo(SqlJoinKind.Anti));
        Assert.That(anti.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.Anti }));
        Assert.That(apply.Kind, Is.EqualTo(SqlJoinKind.OuterApply));
        Assert.That(apply.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.Outer, SqlJoinModifierKind.Apply }));
        Assert.That(asOf.Kind, Is.EqualTo(SqlJoinKind.AsOf));
        Assert.That(asOf.Modifiers, Is.EqualTo(new SqlJoinModifierKind[] { SqlJoinModifierKind.AsOf }));
        Assert.That(asOf.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.On));
    }

    [Test]
    public void NamedWindows_ExposeFramesAndExclusions()
    {
        SqlQuery query = Parse("select sum(amount) over w from sales window w as (partition by region order by day rows between 2 preceding and current row exclude ties)", SqlDialectKind.SQLite);
        SqlWindowDefinition window = query.SelectCores[0].Windows[0];

        Assert.That(window.Name!.GetText(), Is.EqualTo("w"));
        Assert.That(window.PartitionBy.Length, Is.EqualTo(1));
        Assert.That(window.OrderBy.Length, Is.EqualTo(1));
        Assert.That(window.Frame, Is.Not.Null);
        Assert.That(window.Frame!.Kind, Is.EqualTo(SqlWindowFrameKind.Rows));
        Assert.That(window.Frame.Start.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.Preceding));
        Assert.That(window.Frame.Start.Expression!.Literal!.SourceText, Is.EqualTo("2"));
        Assert.That(window.Frame.End!.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.CurrentRow));
        Assert.That(window.Frame.Exclusion, Is.EqualTo(SqlWindowFrameExclusionKind.Ties));
    }

    [Test]
    public void CompoundsAndCtes_ExposeTypedPartsRecursionAndMaterialization()
    {
        SqlQuery query = Parse("with recursive a as materialized (values (1)), b as not materialized (select 2) select * from a union all select * from b except select 0", SqlDialectKind.SQLite);

        Assert.That(query.IsRecursive, Is.True);
        Assert.That(query.CommonTableExpressions[0].Materialization, Is.EqualTo(SqlCteMaterializationKind.Materialized));
        Assert.That(query.CommonTableExpressions[1].Materialization, Is.EqualTo(SqlCteMaterializationKind.NotMaterialized));
        Assert.That(query.CompoundParts.Length, Is.EqualTo(2));
        Assert.That(query.CompoundParts[0].OperatorKind, Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(query.CompoundParts[0].Core, Is.SameAs(query.SelectCores[1]));
        Assert.That(query.CompoundParts[1].OperatorKind, Is.EqualTo(SqlCompoundOperatorKind.Except));
    }

    [Test]
    public void SqlServerTop_ExposesQuantityParenthesesPercentAndTies()
    {
        SqlQuery query = Parse("select top (10) percent with ties id from items order by id", SqlDialectKind.SqlServer);
        SqlTopClause top = query.SelectCores[0].Top!;

        Assert.That(top.Quantity.Literal!.SourceText, Is.EqualTo("10"));
        Assert.That(top.IsParenthesized, Is.True);
        Assert.That(top.IsPercent, Is.True);
        Assert.That(top.WithTies, Is.True);
        Assert.That(SqlGenerator.GenerateQuery(query), Is.EqualTo("SELECT TOP (10) PERCENT WITH TIES id FROM items ORDER BY id"));
    }

    [Test]
    public void OffsetFetch_ExposesBothQuantitiesDirectionAndTies()
    {
        SqlQuery query = Parse("select id from items order by id offset 5 rows fetch next 10 rows with ties", SqlDialectKind.Ansi);
        SqlOffsetFetchClause clause = query.OffsetFetch!;

        Assert.That(clause.Offset!.Literal!.SourceText, Is.EqualTo("5"));
        Assert.That(clause.Fetch!.Literal!.SourceText, Is.EqualTo("10"));
        Assert.That(clause.Direction, Is.EqualTo(SqlFetchDirectionKind.Next));
        Assert.That(clause.WithTies, Is.True);
        Assert.That(SqlGenerator.GenerateQuery(query), Is.EqualTo("SELECT id FROM items ORDER BY id OFFSET 5 ROWS FETCH NEXT 10 ROWS WITH TIES"));
    }

    [Test]
    public void Walker_VisitsTopOffsetAndFetchQuantities()
    {
        SqlQuery query = Parse("select top (3) id from items order by id offset 5 rows fetch next 10 rows only", SqlDialectKind.SqlServer);
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkQuery(query);

        Assert.That(walker.Expressions, Does.Contain(query.SelectCores[0].Top!.Quantity));
        Assert.That(walker.Expressions, Does.Contain(query.OffsetFetch!.Offset));
        Assert.That(walker.Expressions, Does.Contain(query.OffsetFetch.Fetch));
    }

    [Test]
    public void DialectSpecificClauses_ArePreservedAsUnsupportedMarkers()
    {
        string text = "select * from items qualify row_number() over () = 1 order by id";
        SqlParserContext context = CreateContext(text, SqlDialectKind.Snowflake);
        SqlQuery query = new SqlQueryParser(context).ParseQuery();
        SqlUnsupportedQueryClause clause = query.SelectCores[0].UnsupportedClauses[0];

        Assert.That(clause.Kind, Is.EqualTo(SqlUnsupportedQueryClauseKind.Qualify));
        Assert.That(clause.SourceText, Is.EqualTo("qualify row_number() over () = 1"));
        Assert.That(clause.Tokens.Length, Is.GreaterThan(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDIALECT001"));
        Assert.That(query.OrderBy.Length, Is.EqualTo(1));
        Assert.That(SqlGenerator.GenerateQuery(query), Is.EqualTo("SELECT * FROM items qualify row_number() over () = 1 ORDER BY id"));
    }

    [TestCase("select * from t sample 10", SqlUnsupportedQueryClauseKind.Sample)]
    [TestCase("select * from t pivot (sum(x) for y in (1))", SqlUnsupportedQueryClauseKind.Pivot)]
    [TestCase("select * from t unpivot (x for y in (a))", SqlUnsupportedQueryClauseKind.Unpivot)]
    [TestCase("select * from t connect by id = parent_id", SqlUnsupportedQueryClauseKind.ConnectBy)]
    [TestCase("select * from t model return updated rows", SqlUnsupportedQueryClauseKind.Model)]
    public void DialectSpecificClauseKinds_AreClassified(string text, SqlUnsupportedQueryClauseKind expected)
    {
        SqlQuery query = Parse(text, SqlDialectKind.Generic);

        Assert.That(query.SelectCores[0].UnsupportedClauses.Length, Is.EqualTo(1));
        Assert.That(query.SelectCores[0].UnsupportedClauses[0].Kind, Is.EqualTo(expected));
    }

    static SqlQuery Parse(string text, SqlDialectKind dialectKind)
    {
        SqlParserContext context = CreateContext(text, dialectKind);
        return new SqlQueryParser(context).ParseQuery();
    }

    static SqlParserContext CreateContext(string text, SqlDialectKind dialectKind)
    {
        SqlParseOptions options = SqlParseOptions.Create(SqlDialectCatalog.CreateOptions(dialectKind));
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
