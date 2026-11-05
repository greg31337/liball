using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ParseOptionsTests
{
    [Test]
    public void ParseOptions_DefaultsToGenericDialectAndRecovery()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();

        Assert.That(options.Dialect, Is.Not.Null);
        Assert.That(options.GetDialectKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(options.EnableRecovery, Is.True);
        Assert.That(options.MaxRecoverySteps, Is.EqualTo(1000));
        Assert.That(options.MaxDiagnostics, Is.EqualTo(100));
        Assert.That(options.StopOnFirstError, Is.False);
        Assert.That(options.AllowMultipleStatements, Is.True);
        Assert.That(options.AllowEmptyStatements, Is.True);
        Assert.That(options.PreserveTokens, Is.True);
        Assert.That(options.PreserveTrivia, Is.True);
        Assert.That(options.ShouldRecover(), Is.True);
    }

    [Test]
    public void ParseOptions_CreateSqliteUsesSqliteDialect()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        Assert.That(options.GetDialectKind(), Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(options.Dialect.AllowBracketQuotedIdentifiers, Is.True);
        Assert.That(options.Dialect.AllowBacktickQuotedIdentifiers, Is.True);
        Assert.That(options.Dialect.AllowBlobLiterals, Is.True);
        Assert.That(options.Dialect.AllowUpsertClause, Is.True);
    }

    [Test]
    public void ParseOptions_FactoryStoresProvidedDialect()
    {
        SqlDialectOptions dialect = SqliteDialect.CreateOptions();
        SqlParseOptions options = SqlParseOptions.Create(dialect);

        Assert.That(options.Dialect, Is.SameAs(dialect));
        Assert.That(options.GetDialectKind(), Is.EqualTo(SqlDialectKind.SQLite));
    }

    [Test]
    public void ParseOptions_RecoveryAndDiagnosticLimitsReflectSettings()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();

        Assert.That(options.ShouldRecover(), Is.True);
        Assert.That(options.ShouldStopAfterDiagnosticCount(99), Is.False);
        Assert.That(options.ShouldStopAfterDiagnosticCount(100), Is.True);

        options.StopOnFirstError = true;
        Assert.That(options.ShouldRecover(), Is.False);

        options.StopOnFirstError = false;
        options.EnableRecovery = false;
        Assert.That(options.ShouldRecover(), Is.False);

        options.EnableRecovery = true;
        options.MaxRecoverySteps = 0;
        Assert.That(options.ShouldRecover(), Is.False);

        options.MaxDiagnostics = 0;
        Assert.That(options.ShouldStopAfterDiagnosticCount(0), Is.True);
    }

    [Test]
    public void ParseOptions_CopyPreservesSettings()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.EnableRecovery = false;
        options.MaxRecoverySteps = 5;
        options.MaxDiagnostics = 7;
        options.StopOnFirstError = true;
        options.AllowMultipleStatements = false;
        options.AllowEmptyStatements = false;
        options.PreserveTokens = false;
        options.PreserveTrivia = false;

        SqlParseOptions copy = options.Copy();

        Assert.That(copy, Is.Not.SameAs(options));
        Assert.That(copy.Dialect, Is.SameAs(options.Dialect));
        Assert.That(copy.EnableRecovery, Is.False);
        Assert.That(copy.MaxRecoverySteps, Is.EqualTo(5));
        Assert.That(copy.MaxDiagnostics, Is.EqualTo(7));
        Assert.That(copy.StopOnFirstError, Is.True);
        Assert.That(copy.AllowMultipleStatements, Is.False);
        Assert.That(copy.AllowEmptyStatements, Is.False);
        Assert.That(copy.PreserveTokens, Is.False);
        Assert.That(copy.PreserveTrivia, Is.False);
    }

    [Test]
    public void ParseOptions_ValidateRejectsInvalidSettings()
    {
        Assert.Throws<ArgumentNullException>(() => SqlParseOptions.Create(null!));

        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.Dialect = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = SqlParseOptions.CreateDefault();
        options.MaxRecoverySteps = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        options = SqlParseOptions.CreateDefault();
        options.MaxDiagnostics = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        options = SqlParseOptions.CreateDefault();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.ShouldStopAfterDiagnosticCount(-1));
    }

    [Test]
    public void ParseOptions_CreateDefaultReturnsIndependentDialectOptions()
    {
        SqlParseOptions first = SqlParseOptions.CreateDefault();
        SqlParseOptions second = SqlParseOptions.CreateDefault();

        first.Dialect.Keywords.Add("ONLY_FIRST_PARSE_OPTIONS");

        Assert.That(first.Dialect.IsKeyword("ONLY_FIRST_PARSE_OPTIONS"), Is.True);
        Assert.That(second.Dialect.IsKeyword("ONLY_FIRST_PARSE_OPTIONS"), Is.False);
    }
}
