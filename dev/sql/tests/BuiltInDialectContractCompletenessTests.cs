using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class BuiltInDialectContractCompletenessTests
{
    static readonly SqlDialectKind[] Kinds = new SqlDialectKind[]
    {
        SqlDialectKind.Generic,
        SqlDialectKind.Ansi,
        SqlDialectKind.PostgreSql,
        SqlDialectKind.MySql,
        SqlDialectKind.MariaDb,
        SqlDialectKind.SqlServer,
        SqlDialectKind.Oracle,
        SqlDialectKind.DuckDb,
        SqlDialectKind.BigQuery,
        SqlDialectKind.Snowflake,
        SqlDialectKind.Redshift,
        SqlDialectKind.Databricks
    };

    [TestCase(SqlDialectKind.Generic, "SELECT")]
    [TestCase(SqlDialectKind.Ansi, "SELECT")]
    [TestCase(SqlDialectKind.PostgreSql, "BIGSERIAL,ILIKE,JSONB,LATERAL,SERIAL,SIMILAR")]
    [TestCase(SqlDialectKind.MySql, "AUTO_INCREMENT,DATABASE,DUPLICATE,ENGINE,LIMIT,SIGNED,UNSIGNED")]
    [TestCase(SqlDialectKind.MariaDb, "AUTO_INCREMENT,DATABASE,DUPLICATE,ENGINE,LIMIT,SIGNED,UNSIGNED")]
    [TestCase(SqlDialectKind.SqlServer, "IDENTITY,MERGE,NVARCHAR,OFFSET,OUTPUT,TOP")]
    [TestCase(SqlDialectKind.Oracle, "CONNECT,MINUS,NOCYCLE,NUMBER,ROWNUM,START,VARCHAR2")]
    [TestCase(SqlDialectKind.DuckDb, "EXCLUDE,QUALIFY,STRUCT,UNNEST")]
    [TestCase(SqlDialectKind.BigQuery, "ARRAY,QUALIFY,STRUCT,UNNEST")]
    [TestCase(SqlDialectKind.Snowflake, "IFF,ILIKE,QUALIFY,SAMPLE,VARIANT")]
    [TestCase(SqlDialectKind.Redshift, "DISTKEY,ILIKE,SORTKEY,SUPER")]
    [TestCase(SqlDialectKind.Databricks, "DELTA,DISTRIBUTE,LATERAL,QUALIFY,STRUCT")]
    public void BuiltInDialects_RecognizeTheirCompleteDistinctiveKeywordSet(SqlDialectKind kind, string expectedText)
    {
        ISqlDialectPolicy policy = Policy(kind);
        string[] expected = expectedText.Split(',');

        Assert.That(policy.IsKeyword("select"), Is.True);
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(policy.IsKeyword(expected[index].ToLowerInvariant()), Is.True, expected[index]);
            index = index + 1;
        }
    }

    [Test]
    public void BuiltInDialects_ExposeExactIdentifierStringParameterAndLiteralPolicies()
    {
        int index = 0;
        while (index < Kinds.Length)
        {
            SqlDialectKind kind = Kinds[index];
            ISqlDialectPolicy policy = Policy(kind);

            Assert.That(policy.GetIdentifierQuoteStyles(), Is.EqualTo(ExpectedIdentifierStyles(kind)), kind.ToString());
            Assert.That(policy.GetStringQuoteStyles(), Is.EqualTo(ExpectedStringStyles(kind)), kind.ToString());
            Assert.That(policy.GetParameterStyles(), Is.EqualTo(ExpectedParameterStyles(kind)), kind.ToString());
            Assert.That(policy.GetLiteralPolicies(), Is.EqualTo(ExpectedLiteralPolicies(kind)), kind.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void BuiltInDialects_LexEveryAdvertisedParameterStyle()
    {
        int kindIndex = 0;
        while (kindIndex < Kinds.Length)
        {
            SqlDialectKind kind = Kinds[kindIndex];
            SqlDialectOptions options = new SqlDialectCatalogService().CreateOptions(kind);
            SqlParameterStyle[] styles = Policy(kind).GetParameterStyles();
            int styleIndex = 0;
            while (styleIndex < styles.Length)
            {
                string source = ParameterSource(styles[styleIndex]);
                SqlToken[] tokens = SqlLexer.Tokenize(source, options);
                Assert.That(tokens[0].Kind, Is.EqualTo(SqlTokenKind.Parameter), kind + " " + styles[styleIndex]);
                Assert.That(tokens[0].Text, Is.EqualTo(source));
                styleIndex = styleIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [Test]
    public void BuiltInDialects_LexEveryAdvertisedIdentifierQuoteStyle()
    {
        int kindIndex = 0;
        while (kindIndex < Kinds.Length)
        {
            SqlDialectKind kind = Kinds[kindIndex];
            SqlDialectOptions options = new SqlDialectCatalogService().CreateOptions(kind);
            SqlNameQuoteStyle[] styles = Policy(kind).GetIdentifierQuoteStyles();
            int styleIndex = 0;
            while (styleIndex < styles.Length)
            {
                string source = IdentifierSource(styles[styleIndex]);
                SqlToken[] tokens = SqlLexer.Tokenize(source, options);
                Assert.That(tokens[0].Kind, Is.EqualTo(SqlTokenKind.Identifier), kind + " " + styles[styleIndex]);
                Assert.That(tokens[0].IdentifierQuoteStyle, Is.EqualTo(styles[styleIndex]));
                styleIndex = styleIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [TestCase(SqlDialectKind.Generic, false, false, false)]
    [TestCase(SqlDialectKind.Ansi, false, false, false)]
    [TestCase(SqlDialectKind.PostgreSql, true, true, false)]
    [TestCase(SqlDialectKind.MySql, false, true, false)]
    [TestCase(SqlDialectKind.MariaDb, false, true, false)]
    [TestCase(SqlDialectKind.SqlServer, false, false, true)]
    [TestCase(SqlDialectKind.Oracle, false, false, false)]
    [TestCase(SqlDialectKind.DuckDb, true, true, false)]
    [TestCase(SqlDialectKind.BigQuery, false, false, false)]
    [TestCase(SqlDialectKind.Snowflake, false, false, false)]
    [TestCase(SqlDialectKind.Redshift, false, false, false)]
    [TestCase(SqlDialectKind.Databricks, false, false, false)]
    public void BuiltInDialects_ExposeExactClauseAndStatementFeatures(
        SqlDialectKind kind,
        bool returning,
        bool upsert,
        bool merge)
    {
        ISqlDialectPolicy policy = Policy(kind);

        Assert.That(policy.SupportsClause(SqlDialectClauseKind.CommonTableExpression), Is.True);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Window), Is.True);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Returning), Is.EqualTo(returning));
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Upsert), Is.EqualTo(upsert));
        Assert.That(policy.SupportsStatement(SqlStatementKind.Merge), Is.EqualTo(merge));
        Assert.That(policy.SupportsStatement(SqlStatementKind.Select), Is.True);
    }

    [Test]
    public void BuiltInDialects_ExposeCompleteOperatorPrecedenceContract()
    {
        int index = 0;
        while (index < Kinds.Length)
        {
            ISqlDialectPolicy policy = Policy(Kinds[index]);
            AssertUnaryPrecedence(policy);
            AssertBinaryPrecedence(policy);
            index = index + 1;
        }
    }

    [Test]
    public void BuiltInDialects_UsePrecedenceWhenParsingExpressions()
    {
        int index = 0;
        while (index < Kinds.Length)
        {
            SqlParseOptions options = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(Kinds[index]));
            SqlExpressionParseResult result = new SqlParserService().ParseExpression("a or b and c = d + e * f", options);
            SqlExpression expression = result.Expression;

            Assert.That(result.Diagnostics.HasErrors(), Is.False, Kinds[index].ToString());
            Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Or));
            Assert.That(expression.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.And));
            Assert.That(expression.Right.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Equal));
            Assert.That(expression.Right.Right.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
            Assert.That(expression.Right.Right.Right.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Multiply));
            index = index + 1;
        }
    }

    [Test]
    public void PostgreSql_ExposesAndParsesEveryCustomOperator()
    {
        ISqlDialectPolicy policy = Policy(SqlDialectKind.PostgreSql);
        string[] texts = new string[] { "#>>", "#>", "@>", "<@", "?|", "?&", "#-" };
        int[] precedences = new int[] { 50, 50, 40, 40, 40, 40, 50 };
        int index = 0;
        while (index < texts.Length)
        {
            Assert.That(policy.GetCustomOperatorPrecedence(texts[index]), Is.EqualTo(precedences[index]));
            index = index + 1;
        }

        SqlParseOptions options = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(SqlDialectKind.PostgreSql));
        SqlExpressionParseResult result = new SqlParserService().ParseExpression("payload #>> path #- key @> expected", options);

        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Expression.OperatorText, Is.EqualTo("@>"));
        Assert.That(result.Expression.Left!.OperatorText, Is.EqualTo("#-"));
        Assert.That(result.Expression.Left.Left!.OperatorText, Is.EqualTo("#>>"));
    }

    static ISqlDialectPolicy Policy(SqlDialectKind kind)
    {
        return new SqlDialectCatalogService().GetDialect(kind).CreatePolicy();
    }

    static SqlNameQuoteStyle[] ExpectedIdentifierStyles(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.MySql || kind == SqlDialectKind.MariaDb || kind == SqlDialectKind.BigQuery)
        {
            return new SqlNameQuoteStyle[] { SqlNameQuoteStyle.Backtick };
        }

        if (kind == SqlDialectKind.SqlServer)
        {
            return new SqlNameQuoteStyle[] { SqlNameQuoteStyle.DoubleQuote, SqlNameQuoteStyle.Bracket };
        }

        if (kind == SqlDialectKind.Databricks)
        {
            return new SqlNameQuoteStyle[] { SqlNameQuoteStyle.DoubleQuote, SqlNameQuoteStyle.Backtick };
        }

        return new SqlNameQuoteStyle[] { SqlNameQuoteStyle.DoubleQuote };
    }

    static SqlStringQuoteStyle[] ExpectedStringStyles(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.PostgreSql)
        {
            return new SqlStringQuoteStyle[]
            {
                SqlStringQuoteStyle.SingleQuote,
                SqlStringQuoteStyle.EscapedSingleQuote,
                SqlStringQuoteStyle.DollarQuote,
                SqlStringQuoteStyle.UnicodeSingleQuote
            };
        }

        return new SqlStringQuoteStyle[] { SqlStringQuoteStyle.SingleQuote };
    }

    static SqlParameterStyle[] ExpectedParameterStyles(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.Ansi)
        {
            return new SqlParameterStyle[0];
        }

        if (kind == SqlDialectKind.PostgreSql)
        {
            return new SqlParameterStyle[] { SqlParameterStyle.ColonName, SqlParameterStyle.DollarName, SqlParameterStyle.DollarNumber };
        }

        if (kind == SqlDialectKind.SqlServer)
        {
            return new SqlParameterStyle[] { SqlParameterStyle.Question, SqlParameterStyle.NumberedQuestion, SqlParameterStyle.AtName };
        }

        if (kind == SqlDialectKind.Oracle)
        {
            return new SqlParameterStyle[] { SqlParameterStyle.ColonName };
        }

        if (kind == SqlDialectKind.BigQuery)
        {
            return new SqlParameterStyle[] { SqlParameterStyle.AtName };
        }

        if (kind == SqlDialectKind.Snowflake)
        {
            return new SqlParameterStyle[] { SqlParameterStyle.ColonName, SqlParameterStyle.AtName, SqlParameterStyle.DollarName };
        }

        return new SqlParameterStyle[]
        {
            SqlParameterStyle.Question,
            SqlParameterStyle.NumberedQuestion,
            SqlParameterStyle.ColonName,
            SqlParameterStyle.AtName,
            SqlParameterStyle.DollarName
        };
    }

    static SqlDialectLiteralPolicyKind[] ExpectedLiteralPolicies(SqlDialectKind kind)
    {
        SqlDialectLiteralPolicyKind[] withoutBoolean = new SqlDialectLiteralPolicyKind[]
        {
            SqlDialectLiteralPolicyKind.Null,
            SqlDialectLiteralPolicyKind.Integer,
            SqlDialectLiteralPolicyKind.Decimal,
            SqlDialectLiteralPolicyKind.Exponent,
            SqlDialectLiteralPolicyKind.CurrentDate,
            SqlDialectLiteralPolicyKind.CurrentTime,
            SqlDialectLiteralPolicyKind.CurrentTimestamp,
            SqlDialectLiteralPolicyKind.String
        };
        if (kind == SqlDialectKind.MySql || kind == SqlDialectKind.MariaDb)
        {
            return withoutBoolean;
        }

        return new SqlDialectLiteralPolicyKind[]
        {
            SqlDialectLiteralPolicyKind.Null,
            SqlDialectLiteralPolicyKind.Integer,
            SqlDialectLiteralPolicyKind.Decimal,
            SqlDialectLiteralPolicyKind.Exponent,
            SqlDialectLiteralPolicyKind.CurrentDate,
            SqlDialectLiteralPolicyKind.CurrentTime,
            SqlDialectLiteralPolicyKind.CurrentTimestamp,
            SqlDialectLiteralPolicyKind.String,
            SqlDialectLiteralPolicyKind.Boolean
        };
    }

    static string ParameterSource(SqlParameterStyle style)
    {
        if (style == SqlParameterStyle.Question)
        {
            return "?";
        }

        if (style == SqlParameterStyle.NumberedQuestion)
        {
            return "?12";
        }

        if (style == SqlParameterStyle.ColonName)
        {
            return ":value";
        }

        if (style == SqlParameterStyle.AtName)
        {
            return "@value";
        }

        if (style == SqlParameterStyle.DollarName)
        {
            return "$value";
        }

        return "$12";
    }

    static string IdentifierSource(SqlNameQuoteStyle style)
    {
        if (style == SqlNameQuoteStyle.Bracket)
        {
            return "[order detail]";
        }

        if (style == SqlNameQuoteStyle.Backtick)
        {
            return "`order detail`";
        }

        return "\"order detail\"";
    }

    static void AssertUnaryPrecedence(ISqlDialectPolicy policy)
    {
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.Positive), Is.EqualTo(90));
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.Negative), Is.EqualTo(90));
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.BitwiseNot), Is.EqualTo(90));
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.Not), Is.EqualTo(30));
    }

    static void AssertBinaryPrecedence(ISqlDialectPolicy policy)
    {
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Concat), Is.EqualTo(80));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Multiply), Is.EqualTo(70));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Divide), Is.EqualTo(70));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Modulo), Is.EqualTo(70));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Add), Is.EqualTo(60));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Subtract), Is.EqualTo(60));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.BitwiseAnd), Is.EqualTo(50));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.BitwiseOr), Is.EqualTo(50));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.BitwiseXor), Is.EqualTo(50));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.ShiftLeft), Is.EqualTo(50));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.ShiftRight), Is.EqualTo(50));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Equal), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.NotEqual), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Less), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.LessOrEqual), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Greater), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.GreaterOrEqual), Is.EqualTo(40));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.And), Is.EqualTo(20));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.Or), Is.EqualTo(10));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.JsonExtract), Is.EqualTo(0));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.JsonExtractText), Is.EqualTo(0));
    }
}
