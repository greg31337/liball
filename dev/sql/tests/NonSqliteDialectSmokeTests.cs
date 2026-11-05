using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class NonSqliteDialectSmokeTests
{
    [TestCase(SqlDialectKind.PostgreSql, "ilike")]
    [TestCase(SqlDialectKind.MySql, "auto_increment")]
    [TestCase(SqlDialectKind.SqlServer, "top")]
    [TestCase(SqlDialectKind.Oracle, "minus")]
    [TestCase(SqlDialectKind.DuckDb, "qualify")]
    [TestCase(SqlDialectKind.BigQuery, "unnest")]
    [TestCase(SqlDialectKind.Snowflake, "variant")]
    [TestCase(SqlDialectKind.Redshift, "distkey")]
    [TestCase(SqlDialectKind.Databricks, "delta")]
    [TestCase(SqlDialectKind.Ansi, "select")]
    public void NonSqliteDialects_TokenizeDialectKeywords(SqlDialectKind kind, string keyword)
    {
        SqlDialectOptions options = SqlDialectCatalog.CreateOptions(kind);
        SqlToken[] tokens = SqlLexer.Tokenize(keyword, options);

        AssertToken(tokens[0], SqlTokenKind.Keyword, keyword);
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void NonSqliteDialects_TokenizeAllowedIdentifierQuotes()
    {
        AssertFirstToken(SqlDialectKind.PostgreSql, "\"table name\"", SqlTokenKind.Identifier);
        AssertFirstToken(SqlDialectKind.SqlServer, "[table name]", SqlTokenKind.Identifier);
        AssertFirstToken(SqlDialectKind.MySql, "`table name`", SqlTokenKind.Identifier);
        AssertFirstToken(SqlDialectKind.BigQuery, "`project.dataset.table`", SqlTokenKind.Identifier);
        AssertFirstToken(SqlDialectKind.Databricks, "`delta table`", SqlTokenKind.Identifier);
    }

    [Test]
    public void NonSqliteDialects_RejectDisallowedIdentifierQuotes()
    {
        AssertFirstToken(SqlDialectKind.MySql, "\"value\"", SqlTokenKind.BadToken);
        AssertFirstToken(SqlDialectKind.BigQuery, "\"value\"", SqlTokenKind.BadToken);
        AssertFirstTokenText(SqlDialectKind.PostgreSql, "`value`", SqlTokenKind.BadToken, "`");
        AssertFirstTokenText(SqlDialectKind.Ansi, "`value`", SqlTokenKind.BadToken, "`");
    }

    [Test]
    public void NonSqliteDialects_UseDialectParameterPrefixFlags()
    {
        SqlToken[] ansi = SqlLexer.Tokenize("?1 :name @name $name", SqlDialectCatalog.CreateOptions(SqlDialectKind.Ansi));
        SqlToken[] bigQuery = SqlLexer.Tokenize("@name ?1", SqlDialectCatalog.CreateOptions(SqlDialectKind.BigQuery));
        SqlToken[] postgres = SqlLexer.Tokenize("$name ?1", SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql));

        AssertToken(ansi[0], SqlTokenKind.Question, "?");
        AssertToken(ansi[3], SqlTokenKind.Colon, ":");
        AssertToken(ansi[6], SqlTokenKind.At, "@");
        AssertToken(ansi[9], SqlTokenKind.Dollar, "$");
        AssertToken(bigQuery[0], SqlTokenKind.Parameter, "@name");
        AssertToken(bigQuery[2], SqlTokenKind.Question, "?");
        AssertToken(postgres[0], SqlTokenKind.Parameter, "$name");
        AssertToken(postgres[2], SqlTokenKind.Question, "?");
    }

    [Test]
    public void NonSqliteDialects_DollarIdentifierFlagIsDialectSpecific()
    {
        SqlToken[] postgres = SqlLexer.Tokenize("$tag", SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql));
        SqlDialectOptions postgresNoDollarParameters = SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql);
        postgresNoDollarParameters.AllowDollarParameters = false;
        SqlToken[] postgresIdentifier = SqlLexer.Tokenize("$tag", postgresNoDollarParameters);
        SqlToken[] ansi = SqlLexer.Tokenize("$tag", SqlDialectCatalog.CreateOptions(SqlDialectKind.Ansi));

        AssertToken(postgres[0], SqlTokenKind.Parameter, "$tag");
        AssertToken(postgresIdentifier[0], SqlTokenKind.Identifier, "$tag");
        AssertToken(ansi[0], SqlTokenKind.Dollar, "$");
        AssertToken(ansi[1], SqlTokenKind.Identifier, "tag");
    }

    static void AssertFirstToken(SqlDialectKind kind, string text, SqlTokenKind expectedKind)
    {
        SqlToken[] tokens = SqlLexer.Tokenize(text, SqlDialectCatalog.CreateOptions(kind));

        AssertToken(tokens[0], expectedKind, text);
    }

    static void AssertFirstTokenText(SqlDialectKind kind, string text, SqlTokenKind expectedKind, string expectedText)
    {
        SqlToken[] tokens = SqlLexer.Tokenize(text, SqlDialectCatalog.CreateOptions(kind));

        AssertToken(tokens[0], expectedKind, expectedText);
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
