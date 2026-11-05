using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteDialectTests
{
    [Test]
    public void SqliteDialect_ConstructsNamedDescriptorWithOptions()
    {
        SqliteDialect dialect = new SqliteDialect();

        Assert.That(dialect.Name, Is.EqualTo("SQLite"));
        Assert.That(dialect.Options, Is.Not.Null);
        Assert.That(dialect.Options.Kind, Is.EqualTo(SqlDialectKind.SQLite));
    }

    [Test]
    public void SqliteDialect_EnablesSqliteIdentifierLiteralAndFeatureFlags()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();

        Assert.That(options.AllowBareIdentifiers, Is.True);
        Assert.That(options.AllowDoubleQuotedIdentifiers, Is.True);
        Assert.That(options.AllowBracketQuotedIdentifiers, Is.True);
        Assert.That(options.AllowBacktickQuotedIdentifiers, Is.True);
        Assert.That(options.AllowSingleQuotedStrings, Is.True);
        Assert.That(options.AllowDoubleQuotedStrings, Is.False);
        Assert.That(options.AllowBlobLiterals, Is.True);
        Assert.That(options.AllowHexIntegerLiterals, Is.True);
        Assert.That(options.AllowReturningClause, Is.True);
        Assert.That(options.AllowUpsertClause, Is.True);
    }

    [Test]
    public void SqliteDialect_EnablesSqliteParameterPrefixes()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();

        Assert.That(options.IsParameterPrefixAllowed('?'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed(':'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed('@'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed('$'), Is.True);
    }

    [Test]
    public void SqliteDialect_IncludesBaseAndSqliteSpecificKeywords()
    {
        SqliteDialect dialect = new SqliteDialect();

        Assert.That(dialect.IsKeyword("select"), Is.True);
        Assert.That(dialect.IsKeyword("pragma"), Is.True);
        Assert.That(dialect.IsKeyword("vacuum"), Is.True);
        Assert.That(dialect.IsKeyword("indexed"), Is.True);
        Assert.That(dialect.IsKeyword("returning"), Is.True);
        Assert.That(dialect.IsKeyword("strict"), Is.True);
        Assert.That(dialect.Options.GetCanonicalKeyword("pragma"), Is.EqualTo("PRAGMA"));
    }

    [Test]
    public void SqliteDialect_CreateOptionsReturnsIndependentKeywordTables()
    {
        SqlDialectOptions first = SqliteDialect.CreateOptions();
        SqlDialectOptions second = SqliteDialect.CreateOptions();

        first.Keywords.Add("ONLY_FIRST");

        Assert.That(first.IsKeyword("ONLY_FIRST"), Is.True);
        Assert.That(second.IsKeyword("ONLY_FIRST"), Is.False);
    }
}
