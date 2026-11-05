using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ParserContextTests
{
    [Test]
    public void ParserContext_ConstructsFromTokensWithDefaultOptions()
    {
        SqlParserContext context = SqlParserContext.Create(Tokens());

        Assert.That(context.Options, Is.Not.Null);
        Assert.That(context.Options.GetDialectKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(context.Diagnostics, Is.SameAs(context.Stream.Diagnostics));
        Assert.That(context.SourceText, Is.Null);
        Assert.That(context.RecoveryStepCount, Is.EqualTo(0));
        Assert.That(context.GetPosition(), Is.EqualTo(0));
    }

    [Test]
    public void ParserContext_ConstructsFromStreamAndPreservesDiagnostics()
    {
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlTokenStream stream = SqlTokenStream.Create(Tokens(), diagnostics);
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlSourceText sourceText = SqlSourceText.Create("select value", "query.sql");
        SqlParserContext context = SqlParserContext.Create(stream, options, sourceText);

        Assert.That(context.Stream, Is.SameAs(stream));
        Assert.That(context.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(context.Options, Is.SameAs(options));
        Assert.That(context.SourceText, Is.SameAs(sourceText));
    }

    [Test]
    public void ParserContext_DelegatesTokenNavigation()
    {
        SqlParserContext context = SqlParserContext.Create(Tokens());

        Assert.That(context.Peek().Kind, Is.EqualTo(SqlTokenKind.Keyword));
        Assert.That(context.PeekAtOffset(1).Kind, Is.EqualTo(SqlTokenKind.Identifier));
        Assert.That(context.Match(SqlTokenKind.Identifier), Is.False);
        Assert.That(context.GetPosition(), Is.EqualTo(0));
        Assert.That(context.Expect(SqlTokenKind.Keyword).Text, Is.EqualTo("select"));
        Assert.That(context.GetPosition(), Is.EqualTo(1));
        Assert.That(context.Consume().Text, Is.EqualTo("value"));
        Assert.That(context.IsAtEnd(), Is.True);
    }

    [Test]
    public void ParserContext_AddsDiagnosticsWithSourceTextAndRespectsLimit()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.MaxDiagnostics = 1;
        SqlSourceText sourceText = SqlSourceText.Create("select value", "query.sql");
        SqlParserContext context = SqlParserContext.Create(Tokens(), options, sourceText);

        SqlDiagnostic? first = context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLP001", "first", new SqlTextSpan(0, 6));
        SqlDiagnostic? second = context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLP002", "second", new SqlTextSpan(7, 5));

        Assert.That(first, Is.Not.Null);
        Assert.That(first!.SourceText, Is.SameAs(sourceText));
        Assert.That(second, Is.Null);
        Assert.That(context.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(context.HasDiagnostics(), Is.True);
        Assert.That(context.HasErrors(), Is.True);
        Assert.That(context.IsDiagnosticLimitReached(), Is.True);
    }

    [Test]
    public void ParserContext_AddErrorUsesTokenSpan()
    {
        SqlParserContext context = SqlParserContext.Create(Tokens());
        SqlToken token = context.Peek();

        SqlDiagnostic? diagnostic = context.AddError("SQLP003", "bad token", token);

        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Span, Is.SameAs(token.Span));
        Assert.That(diagnostic.Code, Is.EqualTo("SQLP003"));
    }

    [Test]
    public void ParserContext_AddUnsupportedFeatureUsesDialectDiagnosticCode()
    {
        SqlParserContext context = SqlParserContext.Create(Tokens());
        SqlTextSpan span = new SqlTextSpan(0, 6);

        SqlDiagnostic? diagnostic = context.AddUnsupportedFeature("test feature", span);

        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Code, Is.EqualTo("SQLDIALECT001"));
        Assert.That(diagnostic.Message, Does.Contain("test feature"));
        Assert.That(diagnostic.Span, Is.SameAs(span));
    }

    [Test]
    public void ParserContext_TracksRecoveryStepsAndSynchronizes()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.MaxRecoverySteps = 2;
        SqlParserContext context = SqlParserContext.Create(Tokens(), options, null);

        Assert.That(context.CanRecover(), Is.True);
        Assert.That(context.SkipForRecovery().Text, Is.EqualTo("select"));
        Assert.That(context.RecoveryStepCount, Is.EqualTo(1));

        context.SynchronizeTo(SqlTokenKind.Semicolon);

        Assert.That(context.RecoveryStepCount, Is.EqualTo(2));
        Assert.That(context.CanRecover(), Is.False);
        Assert.That(context.Peek().Kind, Is.EqualTo(SqlTokenKind.EndOfFile));
    }

    [Test]
    public void ParserContext_RecoveryDisabledByOptionsDoesNotConsume()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.EnableRecovery = false;
        SqlParserContext context = SqlParserContext.Create(Tokens(), options, null);

        SqlToken token = context.SkipForRecovery();

        Assert.That(token.Text, Is.EqualTo("select"));
        Assert.That(context.GetPosition(), Is.EqualTo(0));
        Assert.That(context.RecoveryStepCount, Is.EqualTo(0));
        Assert.That(context.CanRecover(), Is.False);
    }

    [Test]
    public void ParserContext_RejectsInvalidInputs()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();
        SqlTokenStream stream = SqlTokenStream.Create(Tokens());

        Assert.Throws<ArgumentNullException>(() => SqlParserContext.Create((SqlToken[])null!, options, null));
        Assert.Throws<ArgumentNullException>(() => SqlParserContext.Create(Tokens(), null!, null));
        Assert.Throws<ArgumentNullException>(() => SqlParserContext.Create((SqlTokenStream)null!, options, null));
        Assert.Throws<ArgumentNullException>(() => SqlParserContext.Create(stream, null!, null));

        options.MaxDiagnostics = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParserContext.Create(Tokens(), options, null));

        SqlParserContext context = SqlParserContext.Create(Tokens());
        Assert.Throws<ArgumentNullException>(() => context.AddError("SQLP004", "bad token", null!));
    }

    static SqlToken[] Tokens()
    {
        return new SqlToken[]
        {
            SqlToken.Create(SqlTokenKind.Keyword, new SqlTextSpan(0, 6), "select"),
            SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(7, 5), "value"),
            SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(12, 0), "")
        };
    }
}
