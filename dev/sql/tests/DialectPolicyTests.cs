using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectPolicyTests
{
    [Test]
    public void BuiltInPolicy_ExposesKeywordsOperatorsAndPrecedence()
    {
        ISqlDialect dialect = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite);
        ISqlDialectPolicy policy = dialect.CreatePolicy();

        string[] keywords = policy.GetKeywords();
        SqlOperatorPolicy[] operators = policy.GetOperators();

        Assert.That(policy.GetKind(), Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(keywords, Does.Contain("SELECT"));
        Assert.That(keywords, Does.Contain("PRAGMA"));
        Assert.That(policy.IsKeyword("select"), Is.True);
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.Negative), Is.EqualTo(90));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Multiply), Is.EqualTo(70));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Add), Is.EqualTo(60));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.And), Is.EqualTo(20));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Or), Is.EqualTo(10));
        Assert.That(operators, Has.Length.GreaterThan(20));
    }

    [Test]
    public void BuiltInPolicy_ExposesQuoteParameterLiteralAndCapabilityPolicies()
    {
        ISqlDialectPolicy sqlite = SqlDialectCatalog.GetDialect(SqlDialectKind.SQLite).CreatePolicy();
        ISqlDialectPolicy generic = SqlDialectCatalog.GetDialect(SqlDialectKind.Generic).CreatePolicy();

        Assert.That(sqlite.GetIdentifierQuoteStyles(), Is.EqualTo(new SqlNameQuoteStyle[]
        {
            SqlNameQuoteStyle.DoubleQuote,
            SqlNameQuoteStyle.Bracket,
            SqlNameQuoteStyle.Backtick
        }));
        Assert.That(sqlite.GetStringQuoteStyles(), Does.Contain(SqlStringQuoteStyle.SingleQuote));
        Assert.That(sqlite.GetParameterStyles(), Does.Contain(SqlParameterStyle.NumberedQuestion));
        Assert.That(sqlite.GetParameterStyles(), Does.Contain(SqlParameterStyle.DollarName));
        Assert.That(sqlite.GetLiteralPolicies(), Does.Contain(SqlDialectLiteralPolicyKind.Blob));
        Assert.That(sqlite.GetLiteralPolicies(), Does.Contain(SqlDialectLiteralPolicyKind.HexInteger));
        Assert.That(sqlite.SupportsStatement(SqlStatementKind.CreateTrigger), Is.True);
        Assert.That(sqlite.SupportsClause(SqlDialectClauseKind.CommonTableExpression), Is.True);
        Assert.That(sqlite.SupportsClause(SqlDialectClauseKind.Window), Is.True);
        Assert.That(sqlite.SupportsClause(SqlDialectClauseKind.Returning), Is.True);
        Assert.That(sqlite.SupportsClause(SqlDialectClauseKind.Upsert), Is.True);
        Assert.That(generic.SupportsClause(SqlDialectClauseKind.Returning), Is.False);
        Assert.That(generic.SupportsClause(SqlDialectClauseKind.Upsert), Is.False);
    }

    [Test]
    public void PolicySnapshot_IsIndependentFromOptionsAndReturnedArrays()
    {
        SqlDialectOptions options = new SqlDialectOptions();
        SqlOperatorPolicy custom = CustomOperator("#>", 55, false);
        options.CustomKeywords = new string[] { "WIDGET" };
        options.CustomOperators = new SqlOperatorPolicy[] { custom };
        ISqlDialectPolicy policy = new SqlDialectPolicy(options);

        options.CustomKeywords[0] = "CHANGED";
        custom.Text = "@@";
        string[] keywords = policy.GetKeywords();
        SqlOperatorPolicy[] operators = policy.GetOperators();
        keywords[0] = "MUTATED";
        operators[operators.Length - 1].Text = "!!";

        Assert.That(policy.IsKeyword("widget"), Is.True);
        Assert.That(policy.IsKeyword("changed"), Is.False);
        Assert.That(policy.GetCustomOperatorPrecedence("#>"), Is.EqualTo(55));
        Assert.That(policy.GetCustomOperatorPrecedence("@@"), Is.EqualTo(0));
        Assert.That(policy.GetKeywords(), Does.Contain("WIDGET"));
        Assert.That(policy.GetOperators()[operators.Length - 1].Text, Is.EqualTo("#>"));
    }

    [Test]
    public void CustomKeywordAndLongestCustomOperator_AffectLexingAndParsing()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.CustomKeywords = new string[] { "WIDGET" };
        dialect.CustomOperators = new SqlOperatorPolicy[]
        {
            CustomOperator("#", 55, false),
            CustomOperator("#>", 55, false)
        };
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        SqlTokenizeResult tokens = SqlParser.Tokenize("widget a #> b", options);
        SqlExpressionParseResult expressionResult = SqlParser.ParseExpression("a #> b + c", options);
        SqlExpression expression = expressionResult.Expression;

        Assert.That(tokens.GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.Keyword));
        Assert.That(tokens.GetTokenAt(4).Kind, Is.EqualTo(SqlTokenKind.CustomOperator));
        Assert.That(tokens.GetTokenAt(4).Text, Is.EqualTo("#>"));
        Assert.That(expressionResult.IsSuccess(), Is.True);
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.OperatorText, Is.EqualTo("#>"));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.None));
        Assert.That(expression.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
    }

    [Test]
    public void RightAssociativeCustomOperator_ControlsPrattGrouping()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.CustomOperators = new SqlOperatorPolicy[] { CustomOperator("**", 75, true) };
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        SqlExpression expression = SqlParser.ParseExpression("a ** b ** c", options).Expression;

        Assert.That(expression.OperatorText, Is.EqualTo("**"));
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Right.OperatorText, Is.EqualTo("**"));
    }

    [Test]
    public void SupportedStatements_ControlUnsupportedFeatureDiagnostics()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.SupportedStatements = new SqlStatementKind[] { SqlStatementKind.Select };
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        SqlParseResult result = SqlStatementParser.ParseScript("delete from items", options);
        ISqlDialectPolicy policy = new SqlDialectPolicy(dialect);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLDIALECT001"));
        Assert.That(policy.SupportsStatement(SqlStatementKind.Select), Is.True);
        Assert.That(policy.SupportsStatement(SqlStatementKind.Delete), Is.False);
    }

    [Test]
    public void CustomPolicies_RejectInvalidDefinitions()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDialectPolicy(null!));
        Assert.Throws<ArgumentNullException>(() => new SqlOperatorPolicy(null!, SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false));
        Assert.Throws<ArgumentException>(() => new SqlOperatorPolicy(" ", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false));
        Assert.Throws<ArgumentException>(() => new SqlOperatorPolicy("+", SqlTokenKind.Plus, SqlUnaryOperatorKind.Positive, SqlBinaryOperatorKind.Add, 10, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 0, false));

        SqlDialectOptions nullKeywords = new SqlDialectOptions();
        nullKeywords.CustomKeywords = null!;
        Assert.Throws<ArgumentNullException>(() => new SqlDialectPolicy(nullKeywords));

        SqlDialectOptions invalidOperator = new SqlDialectOptions();
        invalidOperator.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("word", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false)
        };
        Assert.Throws<ArgumentException>(() => new SqlDialectPolicy(invalidOperator));
    }

    static SqlOperatorPolicy CustomOperator(string text, int precedence, bool isRightAssociative)
    {
        return new SqlOperatorPolicy(text, SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, precedence, isRightAssociative);
    }
}
