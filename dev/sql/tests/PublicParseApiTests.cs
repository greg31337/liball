using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PublicParseApiTests
{
    [Test]
    public void SqlParser_ParseScriptReturnsScriptParseResult()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        SqlParseResult result = SqlParser.ParseScript("select 1; select 2", options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.GetStatementCount(), Is.EqualTo(2));
        Assert.That(result.HasSourceText(), Is.True);
        Assert.That(result.GetTokenCount(), Is.GreaterThan(0));
    }

    [Test]
    public void SqlParser_ParseStatementReturnsSingleStatementResult()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        SqlStatementParseResult result = SqlParser.ParseStatement("insert into items values (1)", options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.Statement.Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(result.Statement.Insert, Is.Not.Null);
        Assert.That(result.SourceText.Text, Is.EqualTo("insert into items values (1)"));
        Assert.That(result.GetTokenCount(), Is.GreaterThan(0));
        Assert.That(result.CreateTokenStream().Diagnostics, Is.SameAs(result.Diagnostics));
    }

    [Test]
    public void SqlParser_ParseStatementReportsTrailingStatementTokens()
    {
        SqlStatementParseResult result = SqlParser.ParseStatement("select 1; select 2");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Statement.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLPARSE003"));
    }

    [Test]
    public void SqlParser_ParseExpressionReturnsExpressionResult()
    {
        SqlExpressionParseResult result = SqlParser.ParseExpression("1 + 2");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(result.Expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(result.SourceText.Text, Is.EqualTo("1 + 2"));
        Assert.That(result.GetTokenCount(), Is.GreaterThan(0));
        Assert.That(result.CreateTokenStream().Diagnostics, Is.SameAs(result.Diagnostics));
    }

    [Test]
    public void SqlParser_ParseExpressionReportsTrailingExpressionTokens()
    {
        SqlExpressionParseResult result = SqlParser.ParseExpression("1 2");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Expression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLEXPR100"));
    }

    [Test]
    public void SqlParser_TokenizeReturnsTokenizeResultWithDiagnostics()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        SqlTokenizeResult result = SqlParser.Tokenize("X'ABC'", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.GetTokenCount(), Is.EqualTo(2));
        Assert.That(result.GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.BlobLiteral));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.CreateTokenStream().Tokens, Is.SameAs(result.Tokens));
    }

    [Test]
    public void PublicParseResultConstructorsRejectInvalidInputs()
    {
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 6));
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(0, 1));
        SqlToken[] tokens = new SqlToken[] { SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(0, 0), "") };
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlSourceText sourceText = SqlSourceText.Create("select");
        SqlParseOptions options = SqlParseOptions.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(null!, tokens, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(statement, null!, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(statement, new SqlToken[] { null! }, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(statement, tokens, null!, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(statement, tokens, diagnostics, null!, options));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParseResult(statement, tokens, diagnostics, sourceText, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(null!, tokens, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(expression, null!, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(expression, new SqlToken[] { null! }, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(expression, tokens, null!, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(expression, tokens, diagnostics, null!, options));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionParseResult(expression, tokens, diagnostics, sourceText, null!));

        Assert.Throws<ArgumentNullException>(() => new SqlTokenizeResult(null!, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenizeResult(new SqlToken[] { null! }, diagnostics, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenizeResult(tokens, null!, sourceText, options));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenizeResult(tokens, diagnostics, null!, options));
        Assert.Throws<ArgumentNullException>(() => new SqlTokenizeResult(tokens, diagnostics, sourceText, null!));
    }

    [Test]
    public void SqlParser_PublicApisRejectInvalidInputs()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseScript(null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseScript("select 1", null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseStatement(null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseStatement("select 1", null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseExpression(null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.ParseExpression("1", null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.Tokenize(null!));
        Assert.Throws<ArgumentNullException>(() => SqlParser.Tokenize("select", null!));

        options.MaxDiagnostics = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParser.ParseStatement("select 1", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParser.ParseExpression("1", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParser.Tokenize("select", options));
    }
}
