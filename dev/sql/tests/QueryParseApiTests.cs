using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class QueryParseApiTests
{
    [Test]
    public void ParserService_ParseQueryReturnsTreeTokensSourceDiagnosticsAndOptions()
    {
        string text = "with cte as (values (1)) select * from cte union all select 2 order by 1";
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        ISqlParser parser = new SqlParserService();

        SqlQueryParseResult result = parser.ParseQuery(text, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Query.CommonTableExpressions.Length, Is.EqualTo(1));
        Assert.That(result.Query.CompoundParts.Length, Is.EqualTo(1));
        Assert.That(result.Query.OrderBy.Length, Is.EqualTo(1));
        Assert.That(result.SourceText.Text, Is.EqualTo(text));
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.HasTokens(), Is.True);
        Assert.That(result.GetTokenCount(), Is.EqualTo(result.Tokens.Length));
        Assert.That(result.GetTokenAt(result.GetTokenCount() - 1).Kind, Is.EqualTo(SqlTokenKind.EndOfFile));
        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void ParserService_ParseQueryReportsTrailingInput()
    {
        SqlQueryParseResult result = new SqlParserService().ParseQuery("select 1; select 2", SqlParseOptions.CreateSqlite());

        Assert.That(result.Query.SelectCores.Length, Is.EqualTo(1));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLQUERY100"));
        Assert.That(result.GetDiagnosticAt(0).Span.Start, Is.EqualTo("select 1".Length));
    }

    [Test]
    public void ParserService_ParseQueryPreservesRecoveryResultAndOptions()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.EnableRecovery = true;

        SqlQueryParseResult result = new SqlParserService().ParseQuery("select 1 2, 3", options);

        Assert.That(result.Query.SelectCores[0].ResultColumns.Length, Is.EqualTo(3));
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLQUERY030"));
        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.Options.EnableRecovery, Is.True);
    }

    [Test]
    public void QueryParseResultCreatesTokenStreamOverSharedData()
    {
        SqlQueryParseResult result = new SqlParserService().ParseQuery("select 1", SqlParseOptions.CreateSqlite());

        SqlTokenStream stream = result.CreateTokenStream();

        Assert.That(stream.Tokens, Is.SameAs(result.Tokens));
        Assert.That(stream.Diagnostics, Is.SameAs(result.Diagnostics));
        Assert.That(stream.Peek(), Is.SameAs(result.Tokens[0]));
    }

    [Test]
    public void ParserService_ParseQueryRejectsInvalidArgumentsAndOptions()
    {
        SqlParserService parser = new SqlParserService();
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        Assert.Throws<ArgumentNullException>(() => parser.ParseQuery(null!, options));
        Assert.Throws<ArgumentNullException>(() => parser.ParseQuery("select 1", null!));

        options.MaxRecoverySteps = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseQuery("select 1", options));
    }

    [Test]
    public void QueryParseResultRejectsInvalidConstructorData()
    {
        SqlQueryParseResult valid = new SqlParserService().ParseQuery("select 1", SqlParseOptions.CreateSqlite());

        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(null!, valid.Tokens, valid.Diagnostics, valid.SourceText, valid.Options));
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(valid.Query, null!, valid.Diagnostics, valid.SourceText, valid.Options));
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(valid.Query, new SqlToken[] { null! }, valid.Diagnostics, valid.SourceText, valid.Options));
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(valid.Query, valid.Tokens, null!, valid.SourceText, valid.Options));
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(valid.Query, valid.Tokens, valid.Diagnostics, null!, valid.Options));
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParseResult(valid.Query, valid.Tokens, valid.Diagnostics, valid.SourceText, null!));
        Assert.Throws<ArgumentException>(() => new SqlQueryParseResult(valid.Query, new SqlToken[0], valid.Diagnostics, valid.SourceText, valid.Options).CreateTokenStream());
    }
}
