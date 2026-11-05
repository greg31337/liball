using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexicalPublicModelTests
{
    [Test]
    public void PostgreSqlDollarQuotedString_PreservesSourceAndDecodesBody()
    {
        string source = " /*lead*/ $tag$line 'quoted'\n$tag$ --tail";
        SqlTokenizeResult result = Tokenize(source, SqlDialectKind.PostgreSql);
        SqlToken token = result.Tokens[3];

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Tokens[0].Kind, Is.EqualTo(SqlTokenKind.Whitespace));
        Assert.That(result.Tokens[1].Kind, Is.EqualTo(SqlTokenKind.BlockComment));
        Assert.That(result.Tokens[5].Kind, Is.EqualTo(SqlTokenKind.LineComment));
        Assert.That(token.Kind, Is.EqualTo(SqlTokenKind.StringLiteral));
        Assert.That(token.Text, Is.EqualTo("$tag$line 'quoted'\n$tag$"));
        Assert.That(token.Value, Is.EqualTo("line 'quoted'\n"));
        Assert.That(token.StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.DollarQuote));
        Assert.That(token.QuoteDelimiter, Is.EqualTo("$tag$"));
        Assert.That(token.Span.Start, Is.EqualTo(10));
        Assert.That(token.Span.Length, Is.EqualTo(token.Text.Length));
    }

    [Test]
    public void PostgreSqlEscapedAndUnicodeStrings_ExposeDecodedValues()
    {
        SqlTokenizeResult escapedResult = Tokenize("E'a\\n\\'b'", SqlDialectKind.PostgreSql);
        SqlTokenizeResult numericEscapesResult = Tokenize("E'\\x41\\101\\u0042\\U0001F600'", SqlDialectKind.PostgreSql);
        SqlTokenizeResult unicodeResult = Tokenize("U&'d\\0061t\\+01F600'", SqlDialectKind.PostgreSql);
        SqlToken escaped = escapedResult.Tokens[0];
        SqlToken numericEscapes = numericEscapesResult.Tokens[0];
        SqlToken unicode = unicodeResult.Tokens[0];

        Assert.That(escaped.Text, Is.EqualTo("E'a\\n\\'b'"));
        Assert.That(escaped.Value, Is.EqualTo("a\n'b"));
        Assert.That(escaped.StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.EscapedSingleQuote));
        Assert.That(escaped.QuoteDelimiter, Is.EqualTo("E'"));
        Assert.That(numericEscapes.Value, Is.EqualTo("AAB😀"));
        Assert.That(unicode.Text, Is.EqualTo("U&'d\\0061t\\+01F600'"));
        Assert.That(unicode.Value, Is.EqualTo("dat😀"));
        Assert.That(unicode.StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.UnicodeSingleQuote));
        Assert.That(unicode.QuoteDelimiter, Is.EqualTo("U&'"));
    }

    [Test]
    public void NewStringForms_ReportUnterminatedDelimiterDiagnostics()
    {
        SqlTokenizeResult escaped = Tokenize("E'value", SqlDialectKind.PostgreSql);
        SqlTokenizeResult unicode = Tokenize("U&'value", SqlDialectKind.PostgreSql);
        SqlTokenizeResult dollar = Tokenize("$tag$value", SqlDialectKind.PostgreSql);

        Assert.That(escaped.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLLEX013"));
        Assert.That(unicode.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLLEX014"));
        Assert.That(dollar.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLLEX015"));
        Assert.That(escaped.Tokens[0].Span.Length, Is.EqualTo(escaped.Tokens[0].Text.Length));
        Assert.That(unicode.Tokens[0].Span.Length, Is.EqualTo(unicode.Tokens[0].Text.Length));
        Assert.That(dollar.Tokens[0].Span.Length, Is.EqualTo(dollar.Tokens[0].Text.Length));
    }

    [Test]
    public void ParsedStringLiteral_UsesTokenDecodedValueWithoutLosingSource()
    {
        SqlDialectOptions dialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.PostgreSql);
        SqlParseOptions options = new SqlParseOptions();
        options.Dialect = dialect;

        SqlExpressionParseResult result = new SqlParserService().ParseExpression("$q$a''b$q$", options);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Expression.Literal, Is.Not.Null);
        Assert.That(result.Expression.Literal!.SourceText, Is.EqualTo("$q$a''b$q$"));
        Assert.That(result.Expression.Literal.Value, Is.EqualTo("a''b"));
        Assert.That(result.Expression.Literal.Token, Is.SameAs(result.Tokens[0]));
    }

    [Test]
    public void QuotedTokens_ExposeEveryEnabledQuoteStyleAndDecodedValue()
    {
        SqlDialectOptions options = new SqlDialectOptions();
        options.AllowBracketQuotedIdentifiers = true;
        options.AllowBacktickQuotedIdentifiers = true;
        options.AllowDoubleQuotedIdentifiers = true;
        SqlTokenizeResult identifiers = Tokenize("\"a\"\"b\" [c]]d] `e``f`", options);

        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowDoubleQuotedStrings = true;
        SqlTokenizeResult strings = Tokenize("'g''h' \"i\"\"j\"", options);

        AssertQuotedIdentifier(identifiers.Tokens[0], "a\"b", SqlNameQuoteStyle.DoubleQuote, "\"");
        AssertQuotedIdentifier(identifiers.Tokens[2], "c]d", SqlNameQuoteStyle.Bracket, "[]");
        AssertQuotedIdentifier(identifiers.Tokens[4], "e`f", SqlNameQuoteStyle.Backtick, "`");
        Assert.That(strings.Tokens[0].Value, Is.EqualTo("g'h"));
        Assert.That(strings.Tokens[0].StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.SingleQuote));
        Assert.That(strings.Tokens[2].Value, Is.EqualTo("i\"j"));
        Assert.That(strings.Tokens[2].StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.DoubleQuote));
    }

    [Test]
    public void DialectParameters_ExposeFormNameAndOriginalSpelling()
    {
        SqlTokenizeResult generic = Tokenize("? ?12 :name @item $named", new SqlDialectOptions());
        SqlTokenizeResult postgres = Tokenize("$1", SqlDialectKind.PostgreSql);

        AssertParameter(generic.Tokens[0], "?", "", SqlParameterStyle.Question);
        AssertParameter(generic.Tokens[2], "?12", "12", SqlParameterStyle.NumberedQuestion);
        AssertParameter(generic.Tokens[4], ":name", "name", SqlParameterStyle.ColonName);
        AssertParameter(generic.Tokens[6], "@item", "item", SqlParameterStyle.AtName);
        AssertParameter(generic.Tokens[8], "$named", "named", SqlParameterStyle.DollarName);
        AssertParameter(postgres.Tokens[0], "$1", "1", SqlParameterStyle.DollarNumber);
    }

    [Test]
    public void CustomOperator_PreservesSpellingValueTriviaAndSpan()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("#>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 50, false)
        };

        SqlTokenizeResult result = Tokenize("left /* gap */ #> right", dialect);
        SqlToken token = result.Tokens[4];

        Assert.That(result.Tokens[2].Kind, Is.EqualTo(SqlTokenKind.BlockComment));
        Assert.That(token.Kind, Is.EqualTo(SqlTokenKind.CustomOperator));
        Assert.That(token.Text, Is.EqualTo("#>"));
        Assert.That(token.Value, Is.EqualTo("#>"));
        Assert.That(token.Span.Start, Is.EqualTo(15));
        Assert.That(token.Span.Length, Is.EqualTo(2));
    }

    [Test]
    public void PostgreSqlPolicy_ReportsNewStringAndParameterForms()
    {
        ISqlDialectCatalog catalog = new SqlDialectCatalogService();
        ISqlDialectPolicy policy = catalog.GetDialect(SqlDialectKind.PostgreSql).CreatePolicy();

        Assert.That(policy.GetStringQuoteStyles(), Does.Contain(SqlStringQuoteStyle.EscapedSingleQuote));
        Assert.That(policy.GetStringQuoteStyles(), Does.Contain(SqlStringQuoteStyle.UnicodeSingleQuote));
        Assert.That(policy.GetStringQuoteStyles(), Does.Contain(SqlStringQuoteStyle.DollarQuote));
        Assert.That(policy.GetParameterStyles(), Does.Contain(SqlParameterStyle.DollarNumber));
    }

    static SqlTokenizeResult Tokenize(string source, SqlDialectKind kind)
    {
        ISqlDialectCatalog catalog = new SqlDialectCatalogService();
        return Tokenize(source, catalog.CreateOptions(kind));
    }

    static SqlTokenizeResult Tokenize(string source, SqlDialectOptions dialect)
    {
        ISqlParser parser = new SqlParserService();
        SqlParseOptions options = new SqlParseOptions();
        options.Dialect = dialect;
        return parser.Tokenize(source, options);
    }

    static void AssertQuotedIdentifier(SqlToken token, string value, SqlNameQuoteStyle style, string delimiter)
    {
        Assert.That(token.Kind, Is.EqualTo(SqlTokenKind.Identifier));
        Assert.That(token.Value, Is.EqualTo(value));
        Assert.That(token.IdentifierQuoteStyle, Is.EqualTo(style));
        Assert.That(token.QuoteDelimiter, Is.EqualTo(delimiter));
    }

    static void AssertParameter(SqlToken token, string text, string value, SqlParameterStyle style)
    {
        Assert.That(token.Kind, Is.EqualTo(SqlTokenKind.Parameter));
        Assert.That(token.Text, Is.EqualTo(text));
        Assert.That(token.Value, Is.EqualTo(value));
        Assert.That(token.ParameterStyle, Is.EqualTo(style));
        Assert.That(token.Span.Length, Is.EqualTo(text.Length));
    }
}
