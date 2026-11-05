using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionCompletionTests
{
    [Test]
    public void ExistingPostfixAndWildcardForms_PopulateTypedPayloads()
    {
        SqlExpression collate = Parse("name collate nocase", SqlDialectKind.SQLite);
        SqlExpression pattern = Parse("name not like 'a!%' escape '!'", SqlDialectKind.SQLite);
        SqlExpression wildcard = Parse("main.items.*", SqlDialectKind.SQLite);

        Assert.That(collate.CollateData, Is.Not.Null);
        Assert.That(collate.CollateData!.Operand.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(collate.CollateData.Collation.GetText(), Is.EqualTo("nocase"));
        Assert.That(pattern.PatternData, Is.Not.Null);
        Assert.That(pattern.PatternData!.IsNegated, Is.True);
        Assert.That(pattern.PatternData.Pattern.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(pattern.PatternData.Escape, Is.Not.Null);
        Assert.That(wildcard.WildcardData, Is.Not.Null);
        Assert.That(wildcard.WildcardData!.Qualifier, Is.Not.Null);
        Assert.That(wildcard.WildcardData.Qualifier!.GetText(), Is.EqualTo("main.items"));
    }

    [Test]
    public void InPredicate_DistinguishesListSubqueryTableAndFunctionTargets()
    {
        SqlExpression list = Parse("id in (1, 2)", SqlDialectKind.SQLite);
        SqlExpression query = Parse("id in (select id from old_items)", SqlDialectKind.SQLite);
        SqlExpression table = Parse("id in lookup", SqlDialectKind.SQLite);
        SqlExpression function = Parse("id not in json_each(payload)", SqlDialectKind.SQLite);

        Assert.That(list.InData!.TargetKind, Is.EqualTo(SqlInTargetKind.List));
        Assert.That(list.InData.Values.Length, Is.EqualTo(2));
        Assert.That(query.InData!.TargetKind, Is.EqualTo(SqlInTargetKind.Subquery));
        Assert.That(query.InData.Query, Is.Not.Null);
        Assert.That(table.InData!.TargetKind, Is.EqualTo(SqlInTargetKind.Table));
        Assert.That(table.InData.TargetName!.GetText(), Is.EqualTo("lookup"));
        Assert.That(function.InData!.TargetKind, Is.EqualTo(SqlInTargetKind.Function));
        Assert.That(function.InData.IsNegated, Is.True);
        Assert.That(function.InData.TargetName!.GetText(), Is.EqualTo("json_each"));
        Assert.That(function.InData.FunctionArguments.Length, Is.EqualTo(1));
    }

    [Test]
    public void DistinctnessPredicates_HaveDedicatedKindsAndOperands()
    {
        SqlExpression distinct = Parse("a is distinct from b", SqlDialectKind.PostgreSql);
        SqlExpression notDistinct = Parse("a is not distinct from b", SqlDialectKind.PostgreSql);

        Assert.That(distinct.Kind, Is.EqualTo(SqlExpressionKind.Distinctness));
        Assert.That(distinct.DistinctnessData!.Kind, Is.EqualTo(SqlDistinctnessKind.IsDistinctFrom));
        Assert.That(distinct.DistinctnessData.Left.Name!.GetText(), Is.EqualTo("a"));
        Assert.That(distinct.DistinctnessData.Right.Name!.GetText(), Is.EqualTo("b"));
        Assert.That(notDistinct.DistinctnessData!.Kind, Is.EqualTo(SqlDistinctnessKind.IsNotDistinctFrom));
    }

    [Test]
    public void ExtractAndCastForms_PreserveTypedSourceAndSyntax()
    {
        SqlExpression extract = Parse("extract(year from created_at)", SqlDialectKind.PostgreSql);
        SqlExpression standardCast = Parse("cast(total as decimal(10, 2))", SqlDialectKind.PostgreSql);
        SqlExpression postgreSqlCast = Parse("payload::public.jsonb[]", SqlDialectKind.PostgreSql);

        Assert.That(extract.Kind, Is.EqualTo(SqlExpressionKind.Extract));
        Assert.That(extract.ExtractData!.Field, Is.EqualTo("year"));
        Assert.That(extract.ExtractData.Source.Name!.GetText(), Is.EqualTo("created_at"));
        Assert.That(standardCast.CastData!.SyntaxKind, Is.EqualTo(SqlCastSyntaxKind.CastFunction));
        Assert.That(standardCast.CastData.TypeName, Is.EqualTo("decimal(10, 2)"));
        Assert.That(postgreSqlCast.CastData!.SyntaxKind, Is.EqualTo(SqlCastSyntaxKind.PostgreSql));
        Assert.That(postgreSqlCast.CastData.TypeName, Is.EqualTo("public.jsonb[]"));
        Assert.That(postgreSqlCast.CastData.Operand.Name!.GetText(), Is.EqualTo("payload"));
    }

    [Test]
    public void ArraysAndSubscripts_PreserveElementsQueriesIndexesAndSlices()
    {
        SqlExpression indexed = Parse("array[1, :value][2]", SqlDialectKind.PostgreSql);
        SqlExpression sliced = Parse("items[1:3]", SqlDialectKind.PostgreSql);
        SqlExpression query = Parse("array(select id from items)", SqlDialectKind.PostgreSql);

        Assert.That(indexed.Kind, Is.EqualTo(SqlExpressionKind.Subscript));
        Assert.That(indexed.SubscriptData!.Target.Kind, Is.EqualTo(SqlExpressionKind.Array));
        Assert.That(indexed.SubscriptData.Target.ArrayData!.Elements.Length, Is.EqualTo(2));
        Assert.That(indexed.SubscriptData.LowerBound!.Literal, Is.Not.Null);
        Assert.That(sliced.SubscriptData!.IsSlice, Is.True);
        Assert.That(sliced.SubscriptData.LowerBound, Is.Not.Null);
        Assert.That(sliced.SubscriptData.UpperBound, Is.Not.Null);
        Assert.That(query.Kind, Is.EqualTo(SqlExpressionKind.Array));
        Assert.That(query.ArrayData!.Query, Is.Not.Null);

        SqlExpression[] parameters = new SqlAnalyzerService().FindParametersInExpression(indexed);
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Value, Is.EqualTo(":value"));
    }

    [Test]
    public void JsonAndCustomOperators_HaveLosslessTypedPayloads()
    {
        SqlExpression json = Parse("payload #>> '{a}'", SqlDialectKind.PostgreSql);
        SqlDialectOptions customDialect = new SqlDialectOptions();
        customDialect.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("<=>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 45, false)
        };
        SqlExpression custom = Parse("left <=> right", customDialect);

        Assert.That(json.Kind, Is.EqualTo(SqlExpressionKind.JsonOperator));
        Assert.That(json.OperatorData, Is.Not.Null);
        Assert.That(json.OperatorData!.OperatorText, Is.EqualTo("#>>"));
        Assert.That(json.OperatorData.Precedence, Is.EqualTo(50));
        Assert.That(json.OperatorData.Kind, Is.EqualTo(SqlOperatorExpressionKind.Json));
        Assert.That(custom.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(custom.OperatorData, Is.Not.Null);
        Assert.That(custom.OperatorData!.OperatorText, Is.EqualTo("<=>"));
        Assert.That(custom.OperatorData.Precedence, Is.EqualTo(45));
        Assert.That(custom.OperatorData.Kind, Is.EqualTo(SqlOperatorExpressionKind.Custom));
    }

    [Test]
    public void AggregateFilterAndWindowInvocation_PopulateOneTypedPayload()
    {
        SqlExpression expression = Parse("sum(distinct amount order by created desc) filter (where active = 1) over report_window", SqlDialectKind.SQLite);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.FunctionData, Is.Not.Null);
        Assert.That(expression.FunctionData!.Name.GetText(), Is.EqualTo("sum"));
        Assert.That(expression.FunctionData.IsDistinct, Is.True);
        Assert.That(expression.FunctionData.Arguments.Length, Is.EqualTo(1));
        Assert.That(expression.FunctionData.OrderBy.Length, Is.EqualTo(1));
        Assert.That(expression.FunctionData.Filter, Is.Not.Null);
        Assert.That(expression.FunctionData.WindowName, Is.Not.Null);
        Assert.That(expression.FunctionData.WindowName!.GetText(), Is.EqualTo("report_window"));
        Assert.That(expression.FunctionData.WindowDefinition, Is.Null);
    }

    [Test]
    public void Generator_CoversCompletedExpressionForms()
    {
        AssertGenerated("a is not distinct from b", SqlDialectKind.PostgreSql, "a IS NOT DISTINCT FROM b");
        AssertGenerated("extract(year from created_at)", SqlDialectKind.PostgreSql, "EXTRACT(year FROM created_at)");
        AssertGenerated("payload::jsonb", SqlDialectKind.PostgreSql, "payload::jsonb");
        AssertGenerated("array[1, 2][1:2]", SqlDialectKind.PostgreSql, "ARRAY[1, 2][1:2]");
        AssertGenerated("payload ->> 'name'", SqlDialectKind.PostgreSql, "payload ->> 'name'");
        AssertGenerated("id in lookup", SqlDialectKind.SQLite, "id IN lookup");
        AssertGenerated("id in json_each(payload)", SqlDialectKind.SQLite, "id IN json_each(payload)");
    }

    static SqlExpression Parse(string text, SqlDialectKind kind)
    {
        SqlDialectOptions dialect = new SqlDialectCatalogService().CreateOptions(kind);
        return Parse(text, dialect);
    }

    static SqlExpression Parse(string text, SqlDialectOptions dialect)
    {
        SqlParseOptions options = new SqlParseOptions();
        options.Dialect = dialect;
        SqlExpressionParseResult result = new SqlParserService().ParseExpression(text, options);
        Assert.That(result.Diagnostics.HasErrors(), Is.False, DiagnosticMessage(result));
        return result.Expression;
    }

    static void AssertGenerated(string text, SqlDialectKind kind, string expected)
    {
        SqlExpression expression = Parse(text, kind);
        string generated = new SqlGeneratorService().GenerateExpression(expression);
        Assert.That(generated, Is.EqualTo(expected));
    }

    static string DiagnosticMessage(SqlExpressionParseResult result)
    {
        if (result.Diagnostics.GetCount() == 0)
        {
            return "";
        }

        return result.Diagnostics.GetAt(0).Message;
    }
}
