using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectKindTests
{
    [Test]
    public void DialectKind_ValuesAreStable()
    {
        Assert.That((int)SqlDialectKind.Generic, Is.EqualTo(0));
        Assert.That((int)SqlDialectKind.SQLite, Is.EqualTo(1));
        Assert.That((int)SqlDialectKind.PostgreSql, Is.EqualTo(2));
        Assert.That((int)SqlDialectKind.MySql, Is.EqualTo(3));
        Assert.That((int)SqlDialectKind.MariaDb, Is.EqualTo(4));
        Assert.That((int)SqlDialectKind.SqlServer, Is.EqualTo(5));
        Assert.That((int)SqlDialectKind.Oracle, Is.EqualTo(6));
        Assert.That((int)SqlDialectKind.DuckDb, Is.EqualTo(7));
        Assert.That((int)SqlDialectKind.BigQuery, Is.EqualTo(8));
        Assert.That((int)SqlDialectKind.Snowflake, Is.EqualTo(9));
        Assert.That((int)SqlDialectKind.Redshift, Is.EqualTo(10));
        Assert.That((int)SqlDialectKind.Databricks, Is.EqualTo(11));
        Assert.That((int)SqlDialectKind.Ansi, Is.EqualTo(12));
    }

    [Test]
    public void DialectKind_DistinguishesGenericAndConcreteDialects()
    {
        Assert.That(SqlDialectKind.Generic, Is.Not.EqualTo(SqlDialectKind.SQLite));
        Assert.That(SqlDialectKind.PostgreSql, Is.Not.EqualTo(SqlDialectKind.MySql));
        Assert.That(SqlDialectKind.SqlServer, Is.Not.EqualTo(SqlDialectKind.Oracle));
        Assert.That(SqlDialectKind.DuckDb, Is.Not.EqualTo(SqlDialectKind.BigQuery));
        Assert.That(SqlDialectKind.Snowflake, Is.Not.EqualTo(SqlDialectKind.Redshift));
        Assert.That(SqlDialectKind.Databricks, Is.Not.EqualTo(SqlDialectKind.Ansi));
    }
}
