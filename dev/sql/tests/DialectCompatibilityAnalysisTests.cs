using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectCompatibilityAnalysisTests
{
    [Test]
    public void CompatibilityAnalysis_AcceptsSupportedSyntaxWithoutChangingTheAst()
    {
        SqlScript script = Parse("select [id] + 1 from [items] where [active] = true", SqlDialectKind.SQLite);
        SqlStatement statement = script.Statements[0];
        SqlQuery query = statement.Query!;
        SqlExpression expression = query.SelectCores[0].ResultColumns[0].Expression!;
        string sourceText = script.SourceText;
        ISqlDialectPolicy target = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();

        SqlCompatibilityResult result = new SqlAnalyzerService().AnalyzeDialectCompatibility(script, target);

        Assert.That(result.IsCompatible(), Is.True, IssueText(result));
        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.Diagnostics.Diagnostics, Is.Empty);
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.TargetDialect, Is.SameAs(target));
        Assert.That(script.Statements[0], Is.SameAs(statement));
        Assert.That(script.Statements[0].Query, Is.SameAs(query));
        Assert.That(query.SelectCores[0].ResultColumns[0].Expression, Is.SameAs(expression));
        Assert.That(script.SourceText, Is.EqualTo(sourceText));
    }

    [Test]
    public void CompatibilityAnalysis_ReportsUnsupportedStatementsAndClauses()
    {
        SqlScript merge = Parse("merge into users u using changes c on u.id = c.id when matched then update set u.name = c.name", SqlDialectKind.SqlServer);
        SqlScript returning = Parse("insert into users(id) values(1) returning id", SqlDialectKind.SQLite);
        ISqlDialectPolicy generic = SqlDialectCatalog.GetDialect(SqlDialectKind.Generic).CreatePolicy();
        ISqlDialectPolicy sqlite = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();
        ISqlAnalyzer analyzer = new SqlAnalyzerService();

        SqlCompatibilityResult mergeResult = analyzer.AnalyzeDialectCompatibility(merge, sqlite);
        SqlCompatibilityResult genericResult = analyzer.AnalyzeDialectCompatibility(returning, generic);
        SqlCompatibilityResult sqliteResult = analyzer.AnalyzeDialectCompatibility(returning, sqlite);

        AssertHasKindAndCode(mergeResult, SqlCompatibilityIssueKind.UnsupportedStatement, "SQLCOMP001");
        AssertHasKindAndCode(genericResult, SqlCompatibilityIssueKind.UnsupportedClause, "SQLCOMP002");
        Assert.That(sqliteResult.IsCompatible(), Is.True, IssueText(sqliteResult));
    }

    [Test]
    public void CompatibilityAnalysis_ReportsDialectSpecificExpressionForms()
    {
        SqlScript script = Parse("select array[1, 2][1], payload::jsonb from events", SqlDialectKind.PostgreSql);
        ISqlDialectPolicy sqlite = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();
        ISqlDialectPolicy postgreSql = SqlDialectCatalog.GetDialect(SqlDialectKind.PostgreSql).CreatePolicy();
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        SqlCompatibilityResult sqliteResult = analyzer.AnalyzeDialectCompatibility(script, sqlite);
        SqlCompatibilityResult postgreSqlResult = analyzer.AnalyzeDialectCompatibility(script, postgreSql);

        AssertHasKindAndCode(sqliteResult, SqlCompatibilityIssueKind.UnsupportedExpression, "SQLCOMP003");
        Assert.That(CountKind(sqliteResult, SqlCompatibilityIssueKind.UnsupportedExpression), Is.EqualTo(3), IssueText(sqliteResult));
        Assert.That(postgreSqlResult.IsCompatible(), Is.True, IssueText(postgreSqlResult));
    }

    [Test]
    public void CompatibilityAnalysis_ReportsLiteralAndQuotingPolicies()
    {
        SqlScript script = Parse("select x'CAFE', [display_name] from [users]", SqlDialectKind.SQLite);
        ISqlDialectPolicy ansi = SqlDialectCatalog.GetDialect(SqlDialectKind.Ansi).CreatePolicy();

        SqlCompatibilityResult result = new SqlAnalyzerService().AnalyzeDialectCompatibility(script, ansi);

        AssertHasKindAndCode(result, SqlCompatibilityIssueKind.UnsupportedLiteral, "SQLCOMP004");
        AssertHasKindAndCode(result, SqlCompatibilityIssueKind.UnsupportedQuoting, "SQLCOMP005");
        Assert.That(CountKind(result, SqlCompatibilityIssueKind.UnsupportedQuoting), Is.EqualTo(2), IssueText(result));
    }

    [Test]
    public void CompatibilityAnalysis_ReportsOperatorsAgainstTheSelectedPolicy()
    {
        SqlScript script = Parse("select payload #>> '{name}' from events", SqlDialectKind.PostgreSql);
        ISqlDialectPolicy sqlite = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();
        ISqlDialectPolicy postgreSql = SqlDialectCatalog.GetDialect(SqlDialectKind.PostgreSql).CreatePolicy();
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        SqlCompatibilityResult sqliteResult = analyzer.AnalyzeDialectCompatibility(script, sqlite);
        SqlCompatibilityResult postgreSqlResult = analyzer.AnalyzeDialectCompatibility(script, postgreSql);

        AssertHasKindAndCode(sqliteResult, SqlCompatibilityIssueKind.UnsupportedOperator, "SQLCOMP006");
        Assert.That(sqliteResult.Issues[0].Feature, Does.Contain("#>>"));
        Assert.That(postgreSqlResult.IsCompatible(), Is.True, IssueText(postgreSqlResult));
    }

    [Test]
    public void CompatibilityAnalysis_ReportsPreservedUnsupportedClausesAndSkippedSyntax()
    {
        SqlScript qualify = ParseWithDiagnostics("select id from users qualify id > 1", SqlDialectKind.Generic);
        SqlScript skipped = ParseWithDiagnostics("select #", SqlDialectKind.SQLite);
        ISqlDialectPolicy sqlite = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        SqlCompatibilityResult qualifyResult = analyzer.AnalyzeDialectCompatibility(qualify, sqlite);
        SqlCompatibilityResult skippedResult = analyzer.AnalyzeDialectCompatibility(skipped, sqlite);

        AssertHasKindAndCode(qualifyResult, SqlCompatibilityIssueKind.UnsupportedClause, "SQLCOMP002");
        AssertHasKindAndCode(skippedResult, SqlCompatibilityIssueKind.UnsupportedSyntax, "SQLCOMP007");
    }

    [Test]
    public void CompatibilityAnalysis_ValidatesRequiredInputsAndOrdersDiagnosticsWithIssues()
    {
        SqlScript script = Parse("select x'AA', [id] from [items]", SqlDialectKind.SQLite);
        ISqlDialectPolicy ansi = SqlDialectCatalog.GetDialect(SqlDialectKind.Ansi).CreatePolicy();
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        SqlCompatibilityResult result = analyzer.AnalyzeDialectCompatibility(script, ansi);

        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeDialectCompatibility(null!, ansi));
        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeDialectCompatibility(script, null!));
        Assert.That(result.Diagnostics.Diagnostics, Has.Count.EqualTo(result.Issues.Length));
        int index = 0;
        while (index < result.Issues.Length)
        {
            Assert.That(result.Diagnostics.Diagnostics[index], Is.SameAs(result.Issues[index].Diagnostic));
            if (index > 0)
            {
                Assert.That(result.Issues[index].Span.Start, Is.GreaterThanOrEqualTo(result.Issues[index - 1].Span.Start));
            }

            index = index + 1;
        }
    }

    static SqlScript Parse(string text, SqlDialectKind kind)
    {
        SqlDialectOptions dialect = SqlDialectCatalog.GetDialect(kind).CreateOptions();
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.Create(dialect));
        Assert.That(result.IsSuccess(), Is.True, ParseDiagnosticText(result));
        return result.Script;
    }

    static SqlScript ParseWithDiagnostics(string text, SqlDialectKind kind)
    {
        SqlDialectOptions dialect = SqlDialectCatalog.GetDialect(kind).CreateOptions();
        return SqlParser.ParseScript(text, SqlParseOptions.Create(dialect)).Script;
    }

    static int CountKind(SqlCompatibilityResult result, SqlCompatibilityIssueKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < result.Issues.Length)
        {
            if (result.Issues[index].Kind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static void AssertHasKindAndCode(SqlCompatibilityResult result, SqlCompatibilityIssueKind kind, string code)
    {
        int index = 0;
        while (index < result.Issues.Length)
        {
            if (result.Issues[index].Kind == kind && result.Issues[index].Diagnostic.Code == code)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected " + kind.ToString() + " with code " + code + ".\n" + IssueText(result));
    }

    static string IssueText(SqlCompatibilityResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.Issues.Length)
        {
            SqlCompatibilityIssue issue = result.Issues[index];
            text = text + issue.Diagnostic.Code + ":" + issue.Kind.ToString() + ":" + issue.Feature + "\n";
            index = index + 1;
        }

        return text;
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
}
