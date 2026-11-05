using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteTypeAffinityAnalysisTests
{
    [Test]
    public void SqliteAffinity_InfersLiteralStorageAffinitiesConservatively()
    {
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("1")), Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("true")), Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("1.5")), Is.EqualTo(SqliteTypeAffinityKind.Real));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("'text'")), Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("x'CAFE'")), Is.EqualTo(SqliteTypeAffinityKind.Blob));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("current_timestamp")), Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(analyzer.GetSqliteLiteralAffinity(ParseLiteral("null")), Is.EqualTo(SqliteTypeAffinityKind.Unknown));
    }

    [Test]
    public void SqliteAffinity_DerivesDeclaredColumnAffinitiesAndHonorsExplicitMetadata()
    {
        SqlAnalyzerService analyzer = new SqlAnalyzerService();
        SqlCatalogColumn integer = new SqlCatalogColumn("a", "UNSIGNED BIG INT");
        SqlCatalogColumn text = new SqlCatalogColumn("b", "varchar(100)");
        SqlCatalogColumn blob = new SqlCatalogColumn("c", "");
        SqlCatalogColumn real = new SqlCatalogColumn("d", "DOUBLE PRECISION");
        SqlCatalogColumn numeric = new SqlCatalogColumn("e", "decimal(10, 2)");
        SqlCatalogColumn explicitAffinity = new SqlCatalogColumn("f", "integer");
        explicitAffinity.Affinity = SqliteTypeAffinityKind.Text;

        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(integer), Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(text), Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(blob), Is.EqualTo(SqliteTypeAffinityKind.Blob));
        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(real), Is.EqualTo(SqliteTypeAffinityKind.Real));
        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(numeric), Is.EqualTo(SqliteTypeAffinityKind.Numeric));
        Assert.That(analyzer.GetSqliteCatalogColumnAffinity(explicitAffinity), Is.EqualTo(SqliteTypeAffinityKind.Text));
    }

    [Test]
    public void SqliteAffinity_InfersStableExpressionAndFunctionOutcomes()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlCatalogFunction normalize = new SqlCatalogFunction("normalize", 1, false);
        normalize.ReturnAffinity = SqliteTypeAffinityKind.Real;
        catalog.AddFunction(normalize);
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("users.id"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("users.name || 'x'"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("users.id + 1"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Numeric));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("cast(users.id as real)"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Real));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("length(users.name)"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("normalize(users.name)"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Real));
        Assert.That(analyzer.InferSqliteExpressionAffinity(ParseExpression("?"), catalog), Is.EqualTo(SqliteTypeAffinityKind.Unknown));
    }

    [Test]
    public void SqliteAffinity_AnalyzesResultColumnsAssignmentsAndTargets()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select u.id, u.name || '!' as label, ? as unknown_value from users u; update users set name = upper(name), id = ?");
        SqlStatement updateStatement = script.Statements[1];

        SqliteTypeAffinityResult result = new SqlAnalyzerService().AnalyzeSqliteTypeAffinities(script, catalog);

        SqliteTypeAffinityEntry id = FindLineageEntry(result, SqliteTypeAffinityTargetKind.ResultColumn, "id");
        SqliteTypeAffinityEntry label = FindLineageEntry(result, SqliteTypeAffinityTargetKind.ResultColumn, "label");
        SqliteTypeAffinityEntry unknown = FindLineageEntry(result, SqliteTypeAffinityTargetKind.ResultColumn, "unknown_value");
        SqliteTypeAffinityEntry nameAssignment = FindLineageEntry(result, SqliteTypeAffinityTargetKind.Assignment, "name");
        SqliteTypeAffinityEntry idAssignment = FindLineageEntry(result, SqliteTypeAffinityTargetKind.Assignment, "id");
        Assert.That(id.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(label.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(unknown.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Unknown));
        Assert.That(nameAssignment.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(nameAssignment.TargetAffinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(idAssignment.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Unknown));
        Assert.That(idAssignment.TargetAffinity, Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(result.HasUnknownAffinity(), Is.True);
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Catalog, Is.SameAs(catalog));
        Assert.That(idAssignment.Assignment, Is.SameAs(updateStatement.Update!.Assignments[1]));
    }

    [Test]
    public void SqliteAffinity_ExpandsWildcardResultAffinitiesAndIncludesCatalogColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select u.* from users u");

        SqliteTypeAffinityResult result = new SqlAnalyzerService().AnalyzeSqliteTypeAffinities(script, catalog);

        Assert.That(FindLineageEntry(result, SqliteTypeAffinityTargetKind.ResultColumn, "id").Affinity, Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(FindLineageEntry(result, SqliteTypeAffinityTargetKind.ResultColumn, "name").Affinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(FindCatalogColumnEntry(result, "id").Affinity, Is.EqualTo(SqliteTypeAffinityKind.Integer));
        Assert.That(FindCatalogColumnEntry(result, "name").Affinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
    }

    [Test]
    public void SqliteAffinity_FocusedResultAndAssignmentOperationsUseValueExpressions()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select cast(id as text) as id_text from users; update users set name = lower(name)");
        SqlResultColumn resultColumn = script.Statements[0].Query!.SelectCores[0].ResultColumns[0];
        SqlAssignment assignment = script.Statements[1].Update!.Assignments[0];
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        Assert.That(analyzer.InferSqliteResultColumnAffinity(resultColumn, catalog), Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(analyzer.InferSqliteAssignmentAffinity(assignment, catalog), Is.EqualTo(SqliteTypeAffinityKind.Text));
    }

    [Test]
    public void SqliteAffinity_ValidatesRequiredInputs()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select 1");
        SqlExpression expression = ParseExpression("1");
        SqlLiteral literal = expression.Literal!;
        SqlResultColumn resultColumn = script.Statements[0].Query!.SelectCores[0].ResultColumns[0];
        SqlAssignment assignment = ParseScript("update users set name = 'x'").Statements[0].Update!.Assignments[0];
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeSqliteTypeAffinities(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeSqliteTypeAffinities(script, null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.GetSqliteLiteralAffinity(null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.GetSqliteCatalogColumnAffinity(null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteExpressionAffinity(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteExpressionAffinity(expression, null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteResultColumnAffinity(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteResultColumnAffinity(resultColumn, null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteAssignmentAffinity(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => analyzer.InferSqliteAssignmentAffinity(assignment, null!));
        Assert.That(analyzer.GetSqliteLiteralAffinity(literal), Is.EqualTo(SqliteTypeAffinityKind.Integer));
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
        orders.CreateColumn("amount", "real");
        return catalog;
    }

    static SqlLiteral ParseLiteral(string text)
    {
        SqlExpression expression = ParseExpression(text);
        Assert.That(expression.Literal, Is.Not.Null);
        return expression.Literal!;
    }

    static SqlExpression ParseExpression(string text)
    {
        SqlExpressionParseResult result = SqlParser.ParseExpression(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True, ParseExpressionDiagnosticText(result));
        return result.Expression;
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True, ParseDiagnosticText(result));
        return result.Script;
    }

    static SqliteTypeAffinityEntry FindLineageEntry(SqliteTypeAffinityResult result, SqliteTypeAffinityTargetKind targetKind, string outputName)
    {
        int index = 0;
        while (index < result.Entries.Length)
        {
            SqliteTypeAffinityEntry entry = result.Entries[index];
            if (entry.TargetKind == targetKind && entry.Lineage != null && entry.Lineage.OutputName == outputName)
            {
                return entry;
            }

            index = index + 1;
        }

        Assert.Fail("Missing affinity entry " + targetKind.ToString() + ":" + outputName + ".");
        return null!;
    }

    static SqliteTypeAffinityEntry FindCatalogColumnEntry(SqliteTypeAffinityResult result, string name)
    {
        int index = 0;
        while (index < result.Entries.Length)
        {
            SqliteTypeAffinityEntry entry = result.Entries[index];
            if (entry.TargetKind == SqliteTypeAffinityTargetKind.CatalogColumn && entry.CatalogColumn != null && entry.CatalogColumn.Name == name)
            {
                return entry;
            }

            index = index + 1;
        }

        Assert.Fail("Missing catalog-column affinity entry " + name + ".");
        return null!;
    }

    static string ParseDiagnosticText(SqlParseResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            text = text + result.GetDiagnosticAt(index).Code + ":" + result.GetDiagnosticAt(index).Message + "\n";
            index = index + 1;
        }

        return text;
    }

    static string ParseExpressionDiagnosticText(SqlExpressionParseResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            text = text + result.GetDiagnosticAt(index).Code + ":" + result.GetDiagnosticAt(index).Message + "\n";
            index = index + 1;
        }

        return text;
    }
}
