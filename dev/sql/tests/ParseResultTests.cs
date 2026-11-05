using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ParseResultTests
{
    [Test]
    public void ParseResult_StoresScriptTokensDiagnosticsSourceAndOptions()
    {
        SqlScript script = ScriptWithStatement();
        SqlToken[] tokens = Tokens();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlSourceText sourceText = SqlSourceText.Create("select 1", "query.sql");
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlParseResult result = SqlParseResult.Create(script, tokens, diagnostics, sourceText, options);

        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Tokens, Is.SameAs(tokens));
        Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(result.SourceText, Is.SameAs(sourceText));
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.HasSourceText(), Is.True);
        Assert.That(result.GetStatementCount(), Is.EqualTo(1));
        Assert.That(result.HasTokens(), Is.True);
        Assert.That(result.GetTokenCount(), Is.EqualTo(2));
        Assert.That(result.GetTokenAt(0), Is.SameAs(tokens[0]));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void ParseResult_SimpleFactoryUsesDefaultOptionsAndNoSourceText()
    {
        SqlParseResult result = SqlParseResult.Create(SqlScript.CreateEmpty(), new SqlToken[0], new SqlDiagnosticBag());

        Assert.That(result.Options, Is.Not.Null);
        Assert.That(result.Options.GetDialectKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(result.HasSourceText(), Is.False);
        Assert.That(result.HasTokens(), Is.False);
        Assert.That(result.GetTokenCount(), Is.EqualTo(0));
        Assert.That(result.GetStatementCount(), Is.EqualTo(0));
    }

    [Test]
    public void ParseResult_ReflectsDiagnosticsAndErrors()
    {
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlDiagnostic warning = diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Warning, "warning", new SqlTextSpan(0, 1));
        SqlDiagnostic error = diagnostics.AddDiagnostic(SqlDiagnosticSeverity.Error, "error", new SqlTextSpan(1, 1));
        SqlParseResult result = SqlParseResult.Create(SqlScript.CreateEmpty(), Tokens(), diagnostics);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(result.GetDiagnosticAt(0), Is.SameAs(warning));
        Assert.That(result.GetDiagnosticAt(1), Is.SameAs(error));
    }

    [Test]
    public void ParseResult_CreatesTokenStreamOverResultTokensAndDiagnostics()
    {
        SqlToken[] tokens = Tokens();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlParseResult result = SqlParseResult.Create(SqlScript.CreateEmpty(), tokens, diagnostics);

        SqlTokenStream stream = result.CreateTokenStream();

        Assert.That(stream.Tokens, Is.SameAs(tokens));
        Assert.That(stream.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(stream.Peek(), Is.SameAs(tokens[0]));
    }

    [Test]
    public void ParseResult_RejectsInvalidInputs()
    {
        SqlScript script = SqlScript.CreateEmpty();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlParseOptions options = SqlParseOptions.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlParseResult.Create(null!, new SqlToken[0], diagnostics));
        Assert.Throws<ArgumentNullException>(() => SqlParseResult.Create(script, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => SqlParseResult.Create(script, new SqlToken[] { null! }, diagnostics));
        Assert.Throws<ArgumentNullException>(() => SqlParseResult.Create(script, new SqlToken[0], null!));
        Assert.Throws<ArgumentNullException>(() => SqlParseResult.Create(script, new SqlToken[0], diagnostics, null, null!));

        options.MaxDiagnostics = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParseResult.Create(script, new SqlToken[0], diagnostics, null, options));
    }

    [Test]
    public void ParseResult_CreateTokenStreamRejectsEmptyTokenArray()
    {
        SqlParseResult result = SqlParseResult.Create(SqlScript.CreateEmpty(), new SqlToken[0], new SqlDiagnosticBag());

        Assert.Throws<ArgumentException>(() => result.CreateTokenStream());
    }

    static SqlScript ScriptWithStatement()
    {
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 8));
        return SqlScript.Create(new SqlStatement[] { statement }, new SqlTextSpan(0, 8));
    }

    static SqlToken[] Tokens()
    {
        return new SqlToken[]
        {
            SqlToken.Create(SqlTokenKind.Keyword, new SqlTextSpan(0, 6), "SELECT"),
            SqlToken.Create(SqlTokenKind.EndOfFile, new SqlTextSpan(6, 0), "")
        };
    }
}
