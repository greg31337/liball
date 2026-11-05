using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AnalyzeParserTests
{
    [Test]
    public void StatementParser_AttachesAnalyzeTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("analyze main.items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Analyze));
        Assert.That(statement.Analyze, Is.Not.Null);
        SqlAnalyzeStatement analyze = statement.Analyze!;
        Assert.That(analyze.HasTargetName(), Is.True);
        Assert.That(analyze.TargetName!.GetText(), Is.EqualTo("main.items"));
    }

    [Test]
    public void AnalyzeParser_ParsesBareAnalyze()
    {
        SqlAnalyzeStatement analyze = ParseAnalyze("analyze");

        Assert.That(analyze.HasTargetName(), Is.False);
        Assert.That(analyze.TargetName, Is.Null);
    }

    [Test]
    public void AnalyzeParser_ParsesQuotedIndexTarget()
    {
        SqlAnalyzeStatement analyze = ParseAnalyze("analyze \"idx items\"");

        Assert.That(analyze.HasTargetName(), Is.True);
        Assert.That(analyze.TargetName!.GetText(), Is.EqualTo("idx items"));
        Assert.That(analyze.TargetName.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void AnalyzeParser_ReportsMalformedTarget()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("analyze .", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLANALYZE002"));
        Assert.That(result.Script.GetStatementAt(0).Analyze, Is.TypeOf<SqlAnalyzeStatement>());
    }

    [Test]
    public void AnalyzeParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlAnalyzeParser(null!));
    }

    static SqlAnalyzeStatement ParseAnalyze(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Analyze!;
    }
}
