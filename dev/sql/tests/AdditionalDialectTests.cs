using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AdditionalDialectTests
{
    [Test]
    public void DialectCatalog_CreatesNamedOptionsForConcreteDialects()
    {
        AssertDialect(SqlDialectKind.PostgreSql, "PostgreSQL");
        AssertDialect(SqlDialectKind.MySql, "MySQL");
        AssertDialect(SqlDialectKind.SqlServer, "SQL Server");
        AssertDialect(SqlDialectKind.Oracle, "Oracle");
        AssertDialect(SqlDialectKind.DuckDb, "DuckDB");
        AssertDialect(SqlDialectKind.BigQuery, "BigQuery");
        AssertDialect(SqlDialectKind.Snowflake, "Snowflake");
        AssertDialect(SqlDialectKind.Redshift, "Redshift");
        AssertDialect(SqlDialectKind.Databricks, "Databricks");
        AssertDialect(SqlDialectKind.Ansi, "ANSI");
        AssertDialect(SqlDialectKind.MariaDb, "MariaDB");
    }

    [Test]
    public void DialectDescriptors_ExposeExpectedNamesKindsAndIndependentOptions()
    {
        ISqlDialect dialect = SqlDialectCatalog.GetDialect(SqlDialectKind.PostgreSql);
        SqlDialectOptions first = dialect.CreateOptions();
        SqlDialectOptions second = dialect.CreateOptions();

        Assert.That(dialect.GetName(), Is.EqualTo("PostgreSQL"));
        Assert.That(dialect.GetKind(), Is.EqualTo(SqlDialectKind.PostgreSql));
        Assert.That(first.Kind, Is.EqualTo(SqlDialectKind.PostgreSql));
        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public void AdditionalDialects_ExposeDialectSpecificKeywords()
    {
        Assert.That(Options(SqlDialectKind.PostgreSql).IsKeyword("ilike"), Is.True);
        Assert.That(Options(SqlDialectKind.MySql).IsKeyword("auto_increment"), Is.True);
        Assert.That(Options(SqlDialectKind.SqlServer).IsKeyword("top"), Is.True);
        Assert.That(Options(SqlDialectKind.Oracle).IsKeyword("minus"), Is.True);
        Assert.That(Options(SqlDialectKind.DuckDb).IsKeyword("qualify"), Is.True);
        Assert.That(Options(SqlDialectKind.BigQuery).IsKeyword("unnest"), Is.True);
        Assert.That(Options(SqlDialectKind.Snowflake).IsKeyword("variant"), Is.True);
        Assert.That(Options(SqlDialectKind.Redshift).IsKeyword("distkey"), Is.True);
        Assert.That(Options(SqlDialectKind.Databricks).IsKeyword("delta"), Is.True);
        Assert.That(Options(SqlDialectKind.Ansi).IsKeyword("select"), Is.True);
    }

    [Test]
    public void AdditionalDialects_UseConservativeFeatureFlags()
    {
        SqlDialectOptions postgres = Options(SqlDialectKind.PostgreSql);
        SqlDialectOptions mysql = Options(SqlDialectKind.MySql);
        SqlDialectOptions sqlServer = Options(SqlDialectKind.SqlServer);
        SqlDialectOptions bigQuery = Options(SqlDialectKind.BigQuery);
        SqlDialectOptions ansi = Options(SqlDialectKind.Ansi);

        Assert.That(postgres.AllowReturningClause, Is.True);
        Assert.That(postgres.AllowUpsertClause, Is.True);
        Assert.That(mysql.AllowReturningClause, Is.False);
        Assert.That(mysql.AllowUpsertClause, Is.True);
        Assert.That(sqlServer.AllowReturningClause, Is.False);
        Assert.That(bigQuery.AllowReturningClause, Is.False);
        Assert.That(ansi.AllowReturningClause, Is.False);
        Assert.That(ansi.AllowQuestionParameters, Is.False);
        Assert.That(ansi.AllowColonParameters, Is.False);
        Assert.That(ansi.AllowAtParameters, Is.False);
        Assert.That(ansi.AllowDollarParameters, Is.False);
    }

    [Test]
    public void AdditionalDialects_ExposeIdentifierQuotePolicies()
    {
        SqlDialectOptions mysql = Options(SqlDialectKind.MySql);
        SqlDialectOptions sqlServer = Options(SqlDialectKind.SqlServer);
        SqlDialectOptions bigQuery = Options(SqlDialectKind.BigQuery);
        SqlDialectOptions databricks = Options(SqlDialectKind.Databricks);

        Assert.That(mysql.AllowDoubleQuotedIdentifiers, Is.False);
        Assert.That(mysql.AllowBacktickQuotedIdentifiers, Is.True);
        Assert.That(sqlServer.AllowBracketQuotedIdentifiers, Is.True);
        Assert.That(bigQuery.AllowDoubleQuotedIdentifiers, Is.False);
        Assert.That(bigQuery.AllowBacktickQuotedIdentifiers, Is.True);
        Assert.That(databricks.AllowBacktickQuotedIdentifiers, Is.True);
    }

    [Test]
    public void DialectCatalog_CreateOptionsReturnsIndependentKeywordTables()
    {
        SqlDialectOptions first = SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql);
        SqlDialectOptions second = SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql);

        first.Keywords.Add("ONLY_FIRST_POSTGRES");

        Assert.That(first.IsKeyword("ONLY_FIRST_POSTGRES"), Is.True);
        Assert.That(second.IsKeyword("ONLY_FIRST_POSTGRES"), Is.False);
    }

    static void AssertDialect(SqlDialectKind kind, string name)
    {
        SqlDialectOptions options = SqlDialectCatalog.CreateOptions(kind);

        Assert.That(SqlDialectCatalog.GetName(kind), Is.EqualTo(name));
        Assert.That(options.Kind, Is.EqualTo(kind));
        Assert.That(options.IsKeyword("select"), Is.True);
        Assert.That(options.AllowCommonTableExpressions, Is.True);
        Assert.That(options.AllowWindowFunctions, Is.True);
    }

    static SqlDialectOptions Options(SqlDialectKind kind)
    {
        return SqlDialectCatalog.GetDialect(kind).CreateOptions();
    }
}
