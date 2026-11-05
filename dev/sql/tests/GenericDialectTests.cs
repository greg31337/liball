using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class GenericDialectTests
{
    [Test]
    public void GenericDialect_ConstructsNamedDescriptorWithOptions()
    {
        GenericDialect dialect = new GenericDialect();

        Assert.That(dialect.Name, Is.EqualTo("Generic"));
        Assert.That(dialect.Options, Is.Not.Null);
        Assert.That(dialect.Options.Kind, Is.EqualTo(SqlDialectKind.Generic));
    }

    [Test]
    public void GenericDialect_UsesBaseIdentifierLiteralAndFeatureFlags()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        Assert.That(options.AllowBareIdentifiers, Is.True);
        Assert.That(options.AllowDoubleQuotedIdentifiers, Is.True);
        Assert.That(options.AllowBracketQuotedIdentifiers, Is.False);
        Assert.That(options.AllowBacktickQuotedIdentifiers, Is.False);
        Assert.That(options.AllowSingleQuotedStrings, Is.True);
        Assert.That(options.AllowDoubleQuotedStrings, Is.False);
        Assert.That(options.AllowBlobLiterals, Is.False);
        Assert.That(options.AllowHexIntegerLiterals, Is.False);
        Assert.That(options.AllowBooleanLiterals, Is.True);
        Assert.That(options.AllowCommonTableExpressions, Is.True);
        Assert.That(options.AllowWindowFunctions, Is.True);
        Assert.That(options.AllowReturningClause, Is.False);
        Assert.That(options.AllowUpsertClause, Is.False);
    }

    [Test]
    public void GenericDialect_IncludesBaseKeywordsOnly()
    {
        GenericDialect dialect = new GenericDialect();

        Assert.That(dialect.IsKeyword("select"), Is.True);
        Assert.That(dialect.IsKeyword("where"), Is.True);
        Assert.That(dialect.IsKeyword("pragma"), Is.False);
        Assert.That(dialect.IsKeyword("vacuum"), Is.False);
        Assert.That(dialect.IsKeyword("strict"), Is.False);
    }

    [Test]
    public void GenericDialect_CreateOptionsReturnsIndependentKeywordTables()
    {
        SqlDialectOptions first = GenericDialect.CreateOptions();
        SqlDialectOptions second = GenericDialect.CreateOptions();

        first.Keywords.Add("ONLY_FIRST");

        Assert.That(first.IsKeyword("ONLY_FIRST"), Is.True);
        Assert.That(second.IsKeyword("ONLY_FIRST"), Is.False);
    }
}
