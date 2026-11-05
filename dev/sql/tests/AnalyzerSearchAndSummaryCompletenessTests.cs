using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AnalyzerSearchAndSummaryCompletenessTests
{
    [Test]
    public void AnalyzerService_SearchOperationsCoverEveryPublicInputScope()
    {
        SqlScript script = ParseSqlite("select coalesce(u.name, :fallback) from users u where exists (select max(o.total) from orders o where o.user_id = u.id and o.total > ?)");
        SqlStatement statement = script.Statements[0];
        SqlQuery query = statement.Query!;
        SqlExpression expression = query.SelectCores[0].Where!;
        ISqlAnalyzer analyzer = new SqlAnalyzerService();

        AssertNames(analyzer.FindTablesInScript(script), new string[] { "users", "orders" });
        AssertNames(analyzer.FindTablesInStatement(statement), new string[] { "users", "orders" });
        AssertNames(analyzer.FindTablesInQuery(query), new string[] { "users", "orders" });
        AssertNames(analyzer.FindTablesInExpression(expression), new string[] { "orders" });

        AssertContainsName(analyzer.FindColumnsInScript(script), "u.name");
        AssertContainsName(analyzer.FindColumnsInStatement(statement), "o.user_id");
        AssertContainsName(analyzer.FindColumnsInQuery(query), "u.id");
        AssertContainsName(analyzer.FindColumnsInExpression(expression), "o.total");

        AssertValues(analyzer.FindParametersInScript(script), new string[] { ":fallback", "?" });
        AssertValues(analyzer.FindParametersInStatement(statement), new string[] { ":fallback", "?" });
        AssertValues(analyzer.FindParametersInQuery(query), new string[] { ":fallback", "?" });
        AssertValues(analyzer.FindParametersInExpression(expression), new string[] { "?" });

        AssertFunctions(analyzer.FindFunctionsInScript(script), new string[] { "coalesce", "max" });
        AssertFunctions(analyzer.FindFunctionsInStatement(statement), new string[] { "coalesce", "max" });
        AssertFunctions(analyzer.FindFunctionsInQuery(query), new string[] { "coalesce", "max" });
        AssertFunctions(analyzer.FindFunctionsInExpression(expression), new string[] { "max" });
    }

    [Test]
    public void AnalyzerService_NullSearchInputsReturnIndependentEmptyArrays()
    {
        ISqlAnalyzer analyzer = new SqlAnalyzerService();

        Assert.That(analyzer.FindTablesInScript(null), Is.Empty);
        Assert.That(analyzer.FindTablesInStatement(null), Is.Empty);
        Assert.That(analyzer.FindTablesInQuery(null), Is.Empty);
        Assert.That(analyzer.FindTablesInExpression(null), Is.Empty);
        Assert.That(analyzer.FindColumnsInScript(null), Is.Empty);
        Assert.That(analyzer.FindColumnsInStatement(null), Is.Empty);
        Assert.That(analyzer.FindColumnsInQuery(null), Is.Empty);
        Assert.That(analyzer.FindColumnsInExpression(null), Is.Empty);
        Assert.That(analyzer.FindParametersInScript(null), Is.Empty);
        Assert.That(analyzer.FindParametersInStatement(null), Is.Empty);
        Assert.That(analyzer.FindParametersInQuery(null), Is.Empty);
        Assert.That(analyzer.FindParametersInExpression(null), Is.Empty);
        Assert.That(analyzer.FindFunctionsInScript(null), Is.Empty);
        Assert.That(analyzer.FindFunctionsInStatement(null), Is.Empty);
        Assert.That(analyzer.FindFunctionsInQuery(null), Is.Empty);
        Assert.That(analyzer.FindFunctionsInExpression(null), Is.Empty);
    }

    [Test]
    public void AnalyzerService_MergeSearchAndSummaryIncludeTargetSourceAndActionColumns()
    {
        SqlStatement statement = ParseSqlServer(
            "merge into users u using changes c on u.id = c.user_id when matched then update set u.name = c.new_name when not matched then insert(id, name) values(c.user_id, c.new_name)")
            .Statements[0];
        ISqlAnalyzer analyzer = new SqlAnalyzerService();

        SqlName[] tables = analyzer.FindTablesInStatement(statement);
        SqlName[] columns = analyzer.FindColumnsInStatement(statement);
        SqlStatementSummary summary = analyzer.GetStatementSummary(statement);

        AssertContainsName(tables, "users");
        AssertContainsName(tables, "changes");
        AssertContainsName(columns, "u.name");
        AssertContainsName(columns, "id");
        AssertContainsName(columns, "name");
        AssertContainsName(summary.ReadTables, "changes");
        AssertContainsName(summary.WriteTables, "users");
        Assert.That(summary.HasUnknownEffects, Is.False);
    }

    [Test]
    public void StatementSummary_CreateIndexRecordsItsAffectedTable()
    {
        SqlStatement statement = ParseSqlite("create index idx_users_name on users(name)").Statements[0];

        SqlStatementSummary summary = new SqlAnalyzerService().GetStatementSummary(statement);

        Assert.That(summary.Kind, Is.EqualTo(SqlStatementKind.CreateIndex));
        Assert.That(summary.ReadTables, Is.Empty);
        AssertNames(summary.WriteTables, new string[] { "users" });
        Assert.That(summary.HasUnknownEffects, Is.False);
    }

    static SqlScript ParseSqlite(string source)
    {
        SqlParseResult result = new SqlParserService().ParseScript(source, SqlParseOptions.CreateSqlite());
        Assert.That(result.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(result.Diagnostics));
        return result.Script;
    }

    static SqlScript ParseSqlServer(string source)
    {
        SqlDialectOptions dialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.SqlServer);
        SqlParseResult result = new SqlParserService().ParseScript(source, SqlParseOptions.Create(dialect));
        Assert.That(result.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(result.Diagnostics));
        return result.Script;
    }

    static void AssertNames(SqlName[] actual, string[] expected)
    {
        int index = 0;
        while (index < expected.Length)
        {
            AssertContainsName(actual, expected[index]);
            index = index + 1;
        }

        Assert.That(actual, Has.Length.EqualTo(expected.Length));
    }

    static void AssertContainsName(SqlName[] names, string expected)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].GetText() == expected)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Missing name " + expected + ".");
    }

    static void AssertValues(SqlExpression[] expressions, string[] expected)
    {
        Assert.That(expressions, Has.Length.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(expressions[index].Value, Is.EqualTo(expected[index]));
            index = index + 1;
        }
    }

    static void AssertFunctions(SqlExpression[] expressions, string[] expected)
    {
        Assert.That(expressions, Has.Length.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(expressions[index].Name!.GetText(), Is.EqualTo(expected[index]));
            index = index + 1;
        }
    }

    static string FirstDiagnostic(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.IsEmpty())
        {
            return "";
        }

        return diagnostics.GetAt(0).Code + ": " + diagnostics.GetAt(0).Message;
    }
}
