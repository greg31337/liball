using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserPrimaryTests
{
    [Test]
    public void ExpressionParser_ParsesNumericStringBlobAndKeywordLiterals()
    {
        SqlExpression integer = Parse("123", SqlParseOptions.CreateSqlite());
        SqlExpression decimalValue = Parse("1.5", SqlParseOptions.CreateSqlite());
        SqlExpression stringValue = Parse("'a''b'", SqlParseOptions.CreateSqlite());
        SqlExpression blob = Parse("X'53514C'", SqlParseOptions.CreateSqlite());
        SqlExpression nullValue = Parse("NULL", SqlParseOptions.CreateSqlite());
        SqlExpression trueValue = Parse("TRUE", SqlParseOptions.CreateSqlite());
        SqlExpression currentTimestamp = Parse("CURRENT_TIMESTAMP", SqlParseOptions.CreateSqlite());

        AssertLiteral(integer, SqlLiteralKind.Integer, "123");
        AssertLiteral(decimalValue, SqlLiteralKind.Decimal, "1.5");
        AssertLiteral(stringValue, SqlLiteralKind.String, "'a''b'");
        Assert.That(stringValue.Literal!.Value, Is.EqualTo("a'b"));
        AssertLiteral(blob, SqlLiteralKind.Blob, "X'53514C'");
        AssertLiteral(nullValue, SqlLiteralKind.Null, "NULL");
        Assert.That(nullValue.Literal!.HasValue, Is.True);
        Assert.That(nullValue.Literal.Value, Is.Null);
        AssertLiteral(trueValue, SqlLiteralKind.Boolean, "TRUE");
        Assert.That(trueValue.Literal!.Value, Is.EqualTo(true));
        AssertLiteral(currentTimestamp, SqlLiteralKind.CurrentTimestamp, "CURRENT_TIMESTAMP");
    }

    [Test]
    public void ExpressionParser_ParsesQualifiedNamesAndPreservesQuoteStyle()
    {
        SqlExpression expression = Parse("main.[select]", SqlParseOptions.CreateSqlite());

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.Name, Is.Not.Null);
        Assert.That(expression.Name!.GetPartCount(), Is.EqualTo(2));
        Assert.That(expression.Name.GetText(), Is.EqualTo("main.select"));
        Assert.That(expression.Name.GetSourceText(), Is.EqualTo("main.[select]"));
        Assert.That(expression.Name.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.None));
        Assert.That(expression.Name.GetPartAt(1).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.Bracket));
        Assert.That(expression.Name.GetPartAt(1).Text, Is.EqualTo("select"));
    }

    [Test]
    public void ExpressionParser_ParsesBacktickAndDoubleQuotedIdentifierText()
    {
        SqlExpression backtick = Parse("`a``b`", SqlParseOptions.CreateSqlite());
        SqlExpression quoted = Parse("\"a\"\"b\"", SqlParseOptions.CreateSqlite());

        Assert.That(backtick.Name, Is.Not.Null);
        Assert.That(backtick.Name!.GetPartAt(0).Text, Is.EqualTo("a`b"));
        Assert.That(backtick.Name.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.Backtick));
        Assert.That(quoted.Name, Is.Not.Null);
        Assert.That(quoted.Name!.GetPartAt(0).Text, Is.EqualTo("a\"b"));
        Assert.That(quoted.Name.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void ExpressionParser_ParsesWildcardAndQualifiedWildcard()
    {
        SqlExpression wildcard = Parse("*", SqlParseOptions.CreateSqlite());
        SqlExpression qualified = Parse("main.items.*", SqlParseOptions.CreateSqlite());

        Assert.That(wildcard.Kind, Is.EqualTo(SqlExpressionKind.Wildcard));
        Assert.That(wildcard.Name, Is.Null);
        Assert.That(qualified.Kind, Is.EqualTo(SqlExpressionKind.Wildcard));
        Assert.That(qualified.Name, Is.Not.Null);
        Assert.That(qualified.Name!.GetText(), Is.EqualTo("main.items"));
        Assert.That(qualified.Span.Length, Is.EqualTo("main.items.*".Length));
    }

    [Test]
    public void ExpressionParser_ParsesParameters()
    {
        SqlExpression expression = Parse(":id", SqlParseOptions.CreateSqlite());

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Parameter));
        Assert.That(expression.Value, Is.EqualTo(":id"));
        Assert.That(expression.GetTokenCount(), Is.EqualTo(1));
        Assert.That(expression.GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.Parameter));
    }

    [Test]
    public void ExpressionParser_ReportsDiagnosticForDotWithoutIdentifier()
    {
        SqlParserContext context = Context("main.", SqlParseOptions.CreateSqlite());
        SqlExpressionParser parser = new SqlExpressionParser(context);

        SqlExpression expression = parser.ParseExpression();

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(context.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR002"));
    }

    static void AssertLiteral(SqlExpression expression, SqlLiteralKind kind, string sourceText)
    {
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Literal, Is.Not.Null);
        Assert.That(expression.Literal!.Kind, Is.EqualTo(kind));
        Assert.That(expression.Literal.SourceText, Is.EqualTo(sourceText));
        Assert.That(expression.Literal.HasToken(), Is.True);
    }

    static SqlExpression Parse(string text, SqlParseOptions options)
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context(text, options));
        return parser.ParseExpression();
    }

    static SqlParserContext Context(string text, SqlParseOptions options)
    {
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
