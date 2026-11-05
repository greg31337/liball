using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PublicResultContractTests
{
    [Test]
    public void FragmentAndTokenizeResults_ExposeConsistentStatusAndCursorBehavior()
    {
        SqlStatementParseResult statement = SqlParser.ParseStatement("select 1", SqlParseOptions.CreateSqlite());
        SqlExpressionParseResult expression = SqlParser.ParseExpression("1 + 2", SqlParseOptions.CreateSqlite());
        SqlTokenizeResult tokenize = SqlParser.Tokenize("select", SqlParseOptions.CreateSqlite());

        AssertSuccessfulResult(statement.IsSuccess(), statement.HasDiagnostics(), statement.HasErrors(), statement.HasTokens(), statement.GetTokenCount());
        AssertSuccessfulResult(expression.IsSuccess(), expression.HasDiagnostics(), expression.HasErrors(), expression.HasTokens(), expression.GetTokenCount());
        AssertSuccessfulResult(tokenize.IsSuccess(), tokenize.HasDiagnostics(), tokenize.HasErrors(), tokenize.HasTokens(), tokenize.GetTokenCount());
        Assert.That(statement.GetTokenAt(0), Is.SameAs(statement.Tokens[0]));
        Assert.That(expression.GetTokenAt(0), Is.SameAs(expression.Tokens[0]));
        Assert.That(tokenize.GetTokenAt(0), Is.SameAs(tokenize.Tokens[0]));
        Assert.That(statement.CreateTokenStream().Diagnostics, Is.SameAs(statement.Diagnostics));
        Assert.That(expression.CreateTokenStream().Diagnostics, Is.SameAs(expression.Diagnostics));
        Assert.That(tokenize.CreateTokenStream().Diagnostics, Is.SameAs(tokenize.Diagnostics));

        SqlDiagnostic warning = statement.Diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Warning, "warning", new SqlTextSpan(0, 0));
        SqlDiagnostic expressionError = expression.Diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Error, "error", new SqlTextSpan(0, 0));
        SqlDiagnostic tokenError = tokenize.Diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Error, "error", new SqlTextSpan(0, 0));

        Assert.That(statement.IsSuccess(), Is.True);
        Assert.That(statement.HasDiagnostics(), Is.True);
        Assert.That(statement.HasErrors(), Is.False);
        Assert.That(statement.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(statement.GetDiagnosticAt(0), Is.SameAs(warning));
        Assert.That(expression.IsSuccess(), Is.False);
        Assert.That(expression.HasDiagnostics(), Is.True);
        Assert.That(expression.HasErrors(), Is.True);
        Assert.That(expression.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(expression.GetDiagnosticAt(0), Is.SameAs(expressionError));
        Assert.That(tokenize.IsSuccess(), Is.False);
        Assert.That(tokenize.HasDiagnostics(), Is.True);
        Assert.That(tokenize.HasErrors(), Is.True);
        Assert.That(tokenize.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(tokenize.GetDiagnosticAt(0), Is.SameAs(tokenError));
    }

    [Test]
    public void FragmentAndTokenizeResults_RejectEmptyCursorSourcesAndInvalidOptions()
    {
        SqlStatement statement = SqlParser.ParseStatement("select 1").Statement;
        SqlExpression expression = SqlParser.ParseExpression("1").Expression;
        SqlQuery query = new SqlParserService().ParseQuery("select 1", SqlParseOptions.CreateSqlite()).Query;
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlSourceText source = new SqlSourceText("");
        SqlParseOptions options = new SqlParseOptions();

        Assert.Throws<ArgumentException>(() => new SqlStatementParseResult(statement, new SqlToken[0], diagnostics, source, options).CreateTokenStream());
        Assert.Throws<ArgumentException>(() => new SqlExpressionParseResult(expression, new SqlToken[0], diagnostics, source, options).CreateTokenStream());
        Assert.Throws<ArgumentException>(() => new SqlQueryParseResult(query, new SqlToken[0], diagnostics, source, options).CreateTokenStream());
        Assert.Throws<ArgumentException>(() => new SqlTokenizeResult(new SqlToken[0], diagnostics, source, options).CreateTokenStream());

        options.MaxDiagnostics = -1;
        SqlToken[] tokens = EndOfFileTokens();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlStatementParseResult(statement, tokens, diagnostics, source, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlExpressionParseResult(expression, tokens, diagnostics, source, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlQueryParseResult(query, tokens, diagnostics, source, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTokenizeResult(tokens, diagnostics, source, options));
    }

    [Test]
    public void DiffNormalizationAndCanonicalizationResults_ValidateAllReferencesAndItems()
    {
        SqlScript script = EmptyScript();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => new SqlAstDiffResult(null!, script, new SqlAstDiffEntry[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlAstDiffResult(script, null!, new SqlAstDiffEntry[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlAstDiffResult(script, script, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlAstDiffResult(script, script, new SqlAstDiffEntry[] { null! }));

        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(null!, script, "", new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(script, null!, "", new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(script, script, null!, new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(script, script, "", null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(script, script, "", new SqlNormalizationMarker[] { null! }, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlNormalizationResult(script, script, "", new SqlNormalizationMarker[0], null!));

        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(null!, script, "", new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(script, null!, "", new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(script, script, null!, new SqlNormalizationMarker[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(script, script, "", null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(script, script, "", new SqlNormalizationMarker[] { null! }, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCanonicalizationResult(script, script, "", new SqlNormalizationMarker[0], null!));
    }

    [Test]
    public void CompatibilityResult_StoresIssuesAndValidatesAllInputs()
    {
        SqlScript script = EmptyScript();
        ISqlDialectPolicy policy = SqlDialectCatalog.GetDialect(SqlDialectKind.Ansi).CreatePolicy();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlDiagnostic diagnostic = new SqlDiagnostic(SqlDiagnosticSeverity.Warning, "SQLCOMPAT", "unsupported", new SqlTextSpan(0, 0), null);
        SqlCompatibilityIssue issue = new SqlCompatibilityIssue(SqlCompatibilityIssueKind.UnsupportedStatement, "statement", diagnostic.Span, diagnostic);

        SqlCompatibilityResult compatible = new SqlCompatibilityResult(script, policy, new SqlCompatibilityIssue[0], diagnostics);
        SqlCompatibilityResult incompatible = new SqlCompatibilityResult(script, policy, new SqlCompatibilityIssue[] { issue }, diagnostics);

        Assert.That(compatible.IsCompatible(), Is.True);
        Assert.That(compatible.GetIssueCount(), Is.EqualTo(0));
        Assert.That(incompatible.IsCompatible(), Is.False);
        Assert.That(incompatible.GetIssueCount(), Is.EqualTo(1));
        Assert.That(incompatible.GetIssueAt(0), Is.SameAs(issue));
        Assert.Throws<IndexOutOfRangeException>(() => incompatible.GetIssueAt(1));
        Assert.Throws<ArgumentNullException>(() => new SqlCompatibilityResult(null!, policy, new SqlCompatibilityIssue[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCompatibilityResult(script, null!, new SqlCompatibilityIssue[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCompatibilityResult(script, policy, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCompatibilityResult(script, policy, new SqlCompatibilityIssue[] { null! }, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlCompatibilityResult(script, policy, new SqlCompatibilityIssue[0], null!));
    }

    [Test]
    public void LineageAndAffinityResults_ReportCompletenessAndValidateItems()
    {
        SqlScript script = EmptyScript();
        SqlCatalog catalog = new SqlCatalog();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlColumnLineageEntry known = Lineage(SqlColumnLineageOriginKind.DirectColumn);
        SqlColumnLineageEntry unknown = Lineage(SqlColumnLineageOriginKind.Unknown);
        SqlColumnLineageEntry ambiguous = Lineage(SqlColumnLineageOriginKind.Ambiguous);

        Assert.That(new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[0], diagnostics).IsComplete(), Is.True);
        Assert.That(new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[] { known }, diagnostics).IsComplete(), Is.True);
        Assert.That(new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[] { unknown }, diagnostics).IsComplete(), Is.False);
        Assert.That(new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[] { ambiguous }, diagnostics).IsComplete(), Is.False);
        Assert.Throws<ArgumentNullException>(() => new SqlColumnLineageResult(null!, catalog, new SqlColumnLineageEntry[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnLineageResult(script, null!, new SqlColumnLineageEntry[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnLineageResult(script, catalog, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[] { null! }, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnLineageResult(script, catalog, new SqlColumnLineageEntry[0], null!));

        SqliteTypeAffinityEntry integer = Affinity(SqliteTypeAffinityKind.Integer);
        SqliteTypeAffinityEntry unknownAffinity = Affinity(SqliteTypeAffinityKind.Unknown);
        Assert.That(new SqliteTypeAffinityResult(script, catalog, new SqliteTypeAffinityEntry[0]).HasUnknownAffinity(), Is.False);
        Assert.That(new SqliteTypeAffinityResult(script, catalog, new SqliteTypeAffinityEntry[] { integer }).HasUnknownAffinity(), Is.False);
        Assert.That(new SqliteTypeAffinityResult(script, catalog, new SqliteTypeAffinityEntry[] { integer, unknownAffinity }).HasUnknownAffinity(), Is.True);
        Assert.Throws<ArgumentNullException>(() => new SqliteTypeAffinityResult(null!, catalog, new SqliteTypeAffinityEntry[0]));
        Assert.Throws<ArgumentNullException>(() => new SqliteTypeAffinityResult(script, null!, new SqliteTypeAffinityEntry[0]));
        Assert.Throws<ArgumentNullException>(() => new SqliteTypeAffinityResult(script, catalog, null!));
        Assert.Throws<ArgumentNullException>(() => new SqliteTypeAffinityResult(script, catalog, new SqliteTypeAffinityEntry[] { null! }));
    }

    [Test]
    public void TranspileResult_StoresValuesAndValidatesEveryRequiredReferenceAndArrayItem()
    {
        SqlScript script = EmptyScript();
        SqlDialectOptions read = GenericDialect.CreateOptions();
        SqlDialectOptions write = SqlDialectCatalog.CreateOptions(SqlDialectKind.Ansi);
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlTranspileMapping[] mappings = new SqlTranspileMapping[0];
        SqlUnsupportedSyntax[] unsupported = new SqlUnsupportedSyntax[0];

        SqlTranspileResult result = new SqlTranspileResult(script, script, "", read, write, mappings, unsupported, diagnostics, true);

        Assert.That(result.InputScript, Is.SameAs(script));
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Sql, Is.Empty);
        Assert.That(result.ReadDialect, Is.SameAs(read));
        Assert.That(result.WriteDialect, Is.SameAs(write));
        Assert.That(result.Mappings, Is.SameAs(mappings));
        Assert.That(result.UnsupportedSyntax, Is.SameAs(unsupported));
        Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(result.IsLossless, Is.True);

        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(null!, script, "", read, write, mappings, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, null!, "", read, write, mappings, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, null!, read, write, mappings, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", null!, write, mappings, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, null!, mappings, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, write, null!, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, write, new SqlTranspileMapping[] { null! }, unsupported, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, write, mappings, null!, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, write, mappings, new SqlUnsupportedSyntax[] { null! }, diagnostics, true));
        Assert.Throws<ArgumentNullException>(() => new SqlTranspileResult(script, script, "", read, write, mappings, unsupported, null!, true));
    }

    [Test]
    public void LogicalPlanResult_ValidatesEveryRequiredReference()
    {
        SqlScript script = EmptyScript();
        SqlCatalog catalog = new SqlCatalog();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlLogicalPlanNode root = new SqlLogicalPlanNode(SqlLogicalPlanNodeKind.Script, new SqlLogicalPlanNode[0], script, null, new SqlExpression[0], script.Span, "script", true);

        Assert.Throws<ArgumentNullException>(() => new SqlLogicalPlanResult(null!, catalog, root, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlLogicalPlanResult(script, null!, root, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlLogicalPlanResult(script, catalog, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlLogicalPlanResult(script, catalog, root, null!));
    }

    [Test]
    public void ValidationResult_StatusAndDiagnosticsReflectSharedBag()
    {
        SqlScript script = EmptyScript();
        SqlCatalog catalog = new SqlCatalog();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlValidationOptions options = new SqlValidationOptions();
        options.ValidateTableNames = false;
        SqlValidationResult result = new SqlValidationResult(script, catalog, options, diagnostics);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Options, Is.Not.SameAs(options));
        Assert.That(result.Options.ValidateTableNames, Is.False);

        SqlDiagnostic error = diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Error, "invalid", new SqlTextSpan(0, 0));
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnosticAt(0), Is.SameAs(error));
    }

    static void AssertSuccessfulResult(bool success, bool hasDiagnostics, bool hasErrors, bool hasTokens, int tokenCount)
    {
        Assert.That(success, Is.True);
        Assert.That(hasDiagnostics, Is.False);
        Assert.That(hasErrors, Is.False);
        Assert.That(hasTokens, Is.True);
        Assert.That(tokenCount, Is.GreaterThan(0));
    }

    static SqlScript EmptyScript()
    {
        return new SqlScript(new SqlStatement[0], new SqlTextSpan(0, 0));
    }

    static SqlToken[] EndOfFileTokens()
    {
        return new SqlToken[] { new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(0, 0), "") };
    }

    static SqlColumnLineageEntry Lineage(SqlColumnLineageOriginKind origin)
    {
        return new SqlColumnLineageEntry(SqlColumnLineageTargetKind.QueryOutput, origin, "value", null, null, null, null, null, null, new SqlColumnLineageSource[0], new SqlTextSpan(0, 0));
    }

    static SqliteTypeAffinityEntry Affinity(SqliteTypeAffinityKind affinity)
    {
        return new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.Expression, affinity, SqliteTypeAffinityKind.Unknown, null, null, null, null, null, null);
    }
}
