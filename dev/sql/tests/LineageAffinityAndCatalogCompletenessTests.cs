using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LineageAffinityAndCatalogCompletenessTests
{
    [Test]
    public void ColumnLineage_ResolvesCorrelatedScalarSubqueriesAcrossNestedScopes()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite(
            "select u.id, (select max(o.total) from orders o where o.user_id = u.id) as max_total from users u");
        SqlQuery outerQuery = script.Statements[0].Query!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry id = FindLineage(result, outerQuery, "id");
        SqlColumnLineageEntry maximum = FindLineage(result, outerQuery, "max_total");
        Assert.That(id.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.DirectColumn));
        AssertSource(id, "users", "id");
        Assert.That(maximum.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        AssertSource(maximum, "orders", "total");
        Assert.That(maximum.Sources, Has.Length.EqualTo(1));
        Assert.That(result.IsComplete(), Is.True, LineageDiagnosticText(result));
    }

    [Test]
    public void ColumnLineage_UnknownMetadataFlowsThroughDerivedOutputsWithoutThrowing()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select q.unlisted from (select mystery from external_data) q");
        SqlQuery outerQuery = script.Statements[0].Query!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);
        SqlColumnLineageEntry entry = FindLineage(result, outerQuery, "unlisted");

        Assert.That(entry.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Unknown));
        Assert.That(entry.Sources, Is.Not.Empty);
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(HasLineageCode(result, "SQLLINEAGE001"), Is.True, LineageDiagnosticText(result));
    }

    [TestCase("not users.id", SqliteTypeAffinityKind.Integer)]
    [TestCase("users.id = 1", SqliteTypeAffinityKind.Integer)]
    [TestCase("users.name || 'x'", SqliteTypeAffinityKind.Text)]
    [TestCase("-users.id", SqliteTypeAffinityKind.Numeric)]
    [TestCase("cast(users.id as text)", SqliteTypeAffinityKind.Text)]
    [TestCase("coalesce(users.name, 'x')", SqliteTypeAffinityKind.Text)]
    [TestCase("coalesce(users.name, users.id)", SqliteTypeAffinityKind.Unknown)]
    [TestCase("avg(users.id)", SqliteTypeAffinityKind.Real)]
    [TestCase("abs(users.id)", SqliteTypeAffinityKind.Numeric)]
    [TestCase("mystery(users.id)", SqliteTypeAffinityKind.Unknown)]
    [TestCase("users.id between 1 and 2", SqliteTypeAffinityKind.Integer)]
    [TestCase("users.id in (1, 2)", SqliteTypeAffinityKind.Integer)]
    [TestCase("exists(select 1 from users)", SqliteTypeAffinityKind.Integer)]
    [TestCase("(select id from users)", SqliteTypeAffinityKind.Integer)]
    public void SqliteAffinity_ExpressionKindsReturnConservativeDocumentedAffinities(
        string source,
        SqliteTypeAffinityKind expected)
    {
        SqlExpression expression = ParseExpression(source);

        SqliteTypeAffinityKind actual = new SqlAnalyzerService().InferSqliteExpressionAffinity(expression, CreateCatalog());

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SqliteAffinity_AmbiguousMetadataWithDifferentTypesIsUnknown()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlCatalogTable flags = catalog.FindSchema("main")!.CreateTable("flags");
        flags.CreateColumn("id", "text");

        SqliteTypeAffinityKind affinity = new SqlAnalyzerService().InferSqliteExpressionAffinity(ParseExpression("id"), catalog);

        Assert.That(affinity, Is.EqualTo(SqliteTypeAffinityKind.Unknown));
    }

    [Test]
    public void Catalog_QualificationCaseSensitivityAndGlobalFallbackComposeDuringValidation()
    {
        SqlCatalog catalog = SqlCatalog.Create(true, "main");
        catalog.DefaultCatalogName = "warehouse";
        SqlCatalogSchema analytics = catalog.CreateSchema("Analytics");
        SqlCatalogTable events = analytics.CreateTable("Events");
        events.CreateColumn("EventId", "integer");
        SqlCatalogFunction schemaFunction = new SqlCatalogFunction("Normalize", 1, false);
        analytics.AddFunction(schemaFunction);
        SqlCatalogFunction globalFunction = new SqlCatalogFunction("GlobalValue", 0, false);
        catalog.AddFunction(globalFunction);

        SqlScript valid = ParseSqlite("select Analytics.Normalize(e.EventId), GlobalValue() from warehouse.Analytics.Events e");
        SqlScript invalid = ParseSqlite("select e.eventid from warehouse.Analytics.events e");
        SqlValidationResult validResult = new SqlValidatorService().Validate(valid, catalog);
        SqlValidationResult invalidResult = new SqlValidatorService().Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, ValidationDiagnosticText(validResult));
        Assert.That(HasValidationCode(invalidResult, "SQLVAL001"), Is.True, ValidationDiagnosticText(invalidResult));
        Assert.That(catalog.FindFunctionByName(ParseName("Analytics.Normalize"), 1), Is.SameAs(schemaFunction));
        Assert.That(catalog.FindFunctionByName(ParseName("GlobalValue"), 0), Is.SameAs(globalFunction));
        Assert.That(catalog.FindFunctionByName(ParseName("globalvalue"), 0), Is.Null);
        Assert.That(catalog.FindTableByName(ParseName("warehouse.Analytics.Events")), Is.SameAs(events));
        Assert.That(catalog.FindTableByName(ParseName("warehouse.analytics.Events")), Is.Null);
    }

    static SqlCatalog CreateCatalog()
    {
        SqlCatalog catalog = new SqlCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable users = schema.CreateTable("users");
        users.CreateColumn("id", "integer");
        users.CreateColumn("name", "text");
        SqlCatalogTable orders = schema.CreateTable("orders");
        orders.CreateColumn("id", "integer");
        orders.CreateColumn("user_id", "integer");
        orders.CreateColumn("total", "real");
        return catalog;
    }

    static SqlScript ParseSqlite(string source)
    {
        SqlParseResult result = new SqlParserService().ParseScript(source, SqlParseOptions.CreateSqlite());
        Assert.That(result.Diagnostics.HasErrors(), Is.False, ParseDiagnosticText(result.Diagnostics));
        return result.Script;
    }

    static SqlExpression ParseExpression(string source)
    {
        SqlExpressionParseResult result = new SqlParserService().ParseExpression(source, SqlParseOptions.CreateSqlite());
        Assert.That(result.Diagnostics.HasErrors(), Is.False, ParseDiagnosticText(result.Diagnostics));
        return result.Expression;
    }

    static SqlName ParseName(string source)
    {
        return ParseExpression(source).Name!;
    }

    static SqlColumnLineageEntry FindLineage(SqlColumnLineageResult result, SqlQuery query, string outputName)
    {
        int index = 0;
        while (index < result.Entries.Length)
        {
            SqlColumnLineageEntry entry = result.Entries[index];
            if (entry.TargetKind == SqlColumnLineageTargetKind.QueryOutput && entry.Query == query && entry.OutputName == outputName)
            {
                return entry;
            }

            index = index + 1;
        }

        Assert.Fail("Missing lineage " + outputName + ".");
        return null!;
    }

    static void AssertSource(SqlColumnLineageEntry entry, string tableName, string columnName)
    {
        int index = 0;
        while (index < entry.Sources.Length)
        {
            SqlColumnLineageSource source = entry.Sources[index];
            if (source.Table != null && source.Table.Name == tableName && source.Column != null && source.Column.Name == columnName)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Missing source " + tableName + "." + columnName + ".");
    }

    static bool HasLineageCode(SqlColumnLineageResult result, string code)
    {
        int index = 0;
        while (index < result.Diagnostics.GetCount())
        {
            if (result.Diagnostics.GetAt(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool HasValidationCode(SqlValidationResult result, string code)
    {
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string LineageDiagnosticText(SqlColumnLineageResult result)
    {
        return ParseDiagnosticText(result.Diagnostics);
    }

    static string ValidationDiagnosticText(SqlValidationResult result)
    {
        return ParseDiagnosticText(result.Diagnostics);
    }

    static string ParseDiagnosticText(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.IsEmpty())
        {
            return "";
        }

        return diagnostics.GetAt(0).Code + ":" + diagnostics.GetAt(0).Message;
    }
}
