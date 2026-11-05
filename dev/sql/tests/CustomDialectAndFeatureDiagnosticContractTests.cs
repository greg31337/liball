using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CustomDialectAndFeatureDiagnosticContractTests
{
    [TestCase(SqlDialectKind.Generic, false, false)]
    [TestCase(SqlDialectKind.Ansi, false, false)]
    [TestCase(SqlDialectKind.PostgreSql, true, true)]
    [TestCase(SqlDialectKind.MySql, false, true)]
    [TestCase(SqlDialectKind.MariaDb, false, true)]
    [TestCase(SqlDialectKind.SqlServer, false, false)]
    [TestCase(SqlDialectKind.Oracle, false, false)]
    [TestCase(SqlDialectKind.DuckDb, true, true)]
    [TestCase(SqlDialectKind.BigQuery, false, false)]
    [TestCase(SqlDialectKind.Snowflake, false, false)]
    [TestCase(SqlDialectKind.Redshift, false, false)]
    [TestCase(SqlDialectKind.Databricks, false, false)]
    public void BuiltInDialects_ReportUnsupportedReturningAndUpsertExactlyWhenDisabled(
        SqlDialectKind kind,
        bool supportsReturning,
        bool supportsUpsert)
    {
        SqlParseOptions options = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(kind));

        SqlParseResult returning = new SqlParserService().ParseScript(
            "insert into items(id) values (1) returning id",
            options);
        SqlParseResult upsert = new SqlParserService().ParseScript(
            "insert into items(id) values (1) on conflict(id) do nothing",
            options);

        Assert.That(HasDialectDiagnostic(returning), Is.EqualTo(!supportsReturning), kind + " RETURNING");
        Assert.That(HasDialectDiagnostic(upsert), Is.EqualTo(!supportsUpsert), kind + " upsert");
        Assert.That(returning.Script.GetStatementAt(0).Insert!.HasReturning(), Is.True);
        Assert.That(upsert.Script.GetStatementAt(0).Insert!.HasUpsertClauses(), Is.True);
    }

    [Test]
    public void CustomDialect_AllPolicyDimensionsDriveLexingAndPolicySnapshots()
    {
        SqlDialectOptions dialect = CreateCustomDialect();
        ISqlDialectPolicy policy = new SqlDialectPolicy(dialect);

        Assert.That(policy.IsKeyword("PIPELINE"), Is.True);
        Assert.That(policy.IsKeyword("pipeline"), Is.False);
        Assert.That(policy.GetIdentifierQuoteStyles(), Is.EqualTo(new SqlNameQuoteStyle[]
        {
            SqlNameQuoteStyle.Bracket,
            SqlNameQuoteStyle.Backtick
        }));
        Assert.That(policy.GetStringQuoteStyles(), Is.EqualTo(new SqlStringQuoteStyle[]
        {
            SqlStringQuoteStyle.DoubleQuote
        }));
        Assert.That(policy.GetParameterStyles(), Is.EqualTo(new SqlParameterStyle[]
        {
            SqlParameterStyle.AtName,
            SqlParameterStyle.DollarNumber
        }));
        Assert.That(policy.GetLiteralPolicies(), Does.Contain(SqlDialectLiteralPolicyKind.Blob));
        Assert.That(policy.GetLiteralPolicies(), Does.Contain(SqlDialectLiteralPolicyKind.HexInteger));
        Assert.That(policy.GetLiteralPolicies(), Does.Not.Contain(SqlDialectLiteralPolicyKind.Boolean));
        Assert.That(policy.SupportsStatement(SqlStatementKind.Select), Is.True);
        Assert.That(policy.SupportsStatement(SqlStatementKind.Insert), Is.True);
        Assert.That(policy.SupportsStatement(SqlStatementKind.Delete), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.CommonTableExpression), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Window), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Returning), Is.True);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Upsert), Is.False);

        SqlTokenizeResult result = new SqlParserService().Tokenize(
            "PIPELINE pipeline [a]]b] `c``d` \"text\" @name $12 X'CAFE' 0x2A a <=> b ** c",
            SqlParseOptions.Create(dialect));

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        AssertToken(result, 0, SqlTokenKind.Keyword, SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.None);
        AssertToken(result, 2, SqlTokenKind.Identifier, SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.None);
        AssertToken(result, 4, SqlTokenKind.Identifier, SqlNameQuoteStyle.Bracket, SqlStringQuoteStyle.None, SqlParameterStyle.None);
        AssertToken(result, 6, SqlTokenKind.Identifier, SqlNameQuoteStyle.Backtick, SqlStringQuoteStyle.None, SqlParameterStyle.None);
        AssertToken(result, 8, SqlTokenKind.StringLiteral, SqlNameQuoteStyle.None, SqlStringQuoteStyle.DoubleQuote, SqlParameterStyle.None);
        AssertToken(result, 10, SqlTokenKind.Parameter, SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.AtName);
        AssertToken(result, 12, SqlTokenKind.Parameter, SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.DollarNumber);
        Assert.That(result.Tokens[14].Kind, Is.EqualTo(SqlTokenKind.BlobLiteral));
        Assert.That(result.Tokens[16].Kind, Is.EqualTo(SqlTokenKind.NumericLiteral));
        Assert.That(result.Tokens[20].Kind, Is.EqualTo(SqlTokenKind.CustomOperator));
        Assert.That(result.Tokens[24].Kind, Is.EqualTo(SqlTokenKind.CustomOperator));
    }

    [Test]
    public void CustomDialect_OperatorsControlPrecedenceAndAssociativityTogether()
    {
        SqlDialectOptions dialect = CreateCustomDialect();
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        SqlExpressionParseResult precedenceResult = new SqlParserService().ParseExpression("a <=> b + c", options);
        SqlExpressionParseResult associativityResult = new SqlParserService().ParseExpression("a ** b ** c", options);

        Assert.That(precedenceResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(precedenceResult.Expression.OperatorText, Is.EqualTo("<=>"));
        Assert.That(precedenceResult.Expression.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(associativityResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(associativityResult.Expression.OperatorText, Is.EqualTo("**"));
        Assert.That(associativityResult.Expression.Right!.OperatorText, Is.EqualTo("**"));
    }

    [Test]
    public void CustomDialect_ReportsEveryDisabledStatementAndClauseFeature()
    {
        SqlDialectOptions dialect = CreateCustomDialect();
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        SqlParseResult statement = new SqlParserService().ParseScript("delete from items", options);
        SqlParseResult cte = new SqlParserService().ParseScript("with cte as (select 1) select * from cte", options);
        SqlParseResult window = new SqlParserService().ParseScript("select sum(value) over () from items", options);
        SqlParseResult returning = new SqlParserService().ParseScript("insert into items values (1) returning id", options);
        SqlParseResult upsert = new SqlParserService().ParseScript("insert into items values (1) on conflict do nothing", options);

        Assert.That(HasDialectDiagnostic(statement), Is.True);
        Assert.That(HasDialectDiagnostic(cte), Is.True);
        Assert.That(HasDialectDiagnostic(window), Is.True);
        Assert.That(HasDialectDiagnostic(returning), Is.False);
        Assert.That(HasDialectDiagnostic(upsert), Is.True);
    }

    static SqlDialectOptions CreateCustomDialect()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.CaseInsensitiveKeywords = false;
        dialect.CustomKeywords = new string[] { "PIPELINE" };
        dialect.AllowDoubleQuotedIdentifiers = false;
        dialect.AllowBracketQuotedIdentifiers = true;
        dialect.AllowBacktickQuotedIdentifiers = true;
        dialect.AllowSingleQuotedStrings = false;
        dialect.AllowDoubleQuotedStrings = true;
        dialect.AllowBooleanLiterals = false;
        dialect.AllowBlobLiterals = true;
        dialect.AllowHexIntegerLiterals = true;
        dialect.AllowQuestionParameters = false;
        dialect.AllowColonParameters = false;
        dialect.AllowAtParameters = true;
        dialect.AllowDollarParameters = false;
        dialect.AllowDollarNumberParameters = true;
        dialect.AllowCommonTableExpressions = false;
        dialect.AllowWindowFunctions = false;
        dialect.AllowReturningClause = true;
        dialect.AllowUpsertClause = false;
        dialect.SupportedStatements = new SqlStatementKind[] { SqlStatementKind.Select, SqlStatementKind.Insert };
        dialect.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("<=>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 35, false),
            new SqlOperatorPolicy("**", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 75, true)
        };
        return dialect;
    }

    static void AssertToken(
        SqlTokenizeResult result,
        int index,
        SqlTokenKind kind,
        SqlNameQuoteStyle identifierStyle,
        SqlStringQuoteStyle stringStyle,
        SqlParameterStyle parameterStyle)
    {
        SqlToken token = result.Tokens[index];
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.IdentifierQuoteStyle, Is.EqualTo(identifierStyle));
        Assert.That(token.StringQuoteStyle, Is.EqualTo(stringStyle));
        Assert.That(token.ParameterStyle, Is.EqualTo(parameterStyle));
    }

    static bool HasDialectDiagnostic(SqlParseResult result)
    {
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == "SQLDIALECT001")
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
