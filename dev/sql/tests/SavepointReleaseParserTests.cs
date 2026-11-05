using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SavepointReleaseParserTests
{
    [Test]
    public void StatementParser_AttachesSavepointTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("savepoint sp1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Savepoint));
        Assert.That(statement.Savepoint, Is.Not.Null);
        SqlSavepointStatement savepoint = statement.Savepoint!;
        Assert.That(savepoint.HasName(), Is.True);
        Assert.That(savepoint.Name!.GetText(), Is.EqualTo("sp1"));
    }

    [Test]
    public void StatementParser_AttachesReleaseTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("release savepoint sp1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Release));
        Assert.That(statement.Release, Is.Not.Null);
        SqlReleaseStatement release = statement.Release!;
        Assert.That(release.HasSavepointKeyword, Is.True);
        Assert.That(release.HasName(), Is.True);
        Assert.That(release.Name!.GetText(), Is.EqualTo("sp1"));
    }

    [Test]
    public void ReleaseParser_ParsesWithoutSavepointKeywordAndQuotedName()
    {
        SqlReleaseStatement release = ParseRelease("release \"sp name\"");

        Assert.That(release.HasSavepointKeyword, Is.False);
        Assert.That(release.Name!.GetText(), Is.EqualTo("sp name"));
        Assert.That(release.Name.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void SavepointReleaseParsers_ReportMissingNames()
    {
        SqlParseResult missingSavepoint = SqlStatementParser.ParseScript("savepoint", SqlParseOptions.CreateSqlite());
        SqlParseResult missingRelease = SqlStatementParser.ParseScript("release savepoint", SqlParseOptions.CreateSqlite());

        Assert.That(missingSavepoint.IsSuccess(), Is.False);
        Assert.That(missingSavepoint.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLSAVEPOINT002"));
        Assert.That(missingSavepoint.Script.GetStatementAt(0).Savepoint, Is.TypeOf<SqlSavepointStatement>());
        Assert.That(missingRelease.IsSuccess(), Is.False);
        Assert.That(missingRelease.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLRELEASE002"));
        Assert.That(missingRelease.Script.GetStatementAt(0).Release, Is.TypeOf<SqlReleaseStatement>());
    }

    [Test]
    public void SavepointReleaseParsers_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlSavepointParser(null!));
        Assert.Throws<ArgumentNullException>(() => new SqlReleaseParser(null!));
    }

    static SqlReleaseStatement ParseRelease(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Release!;
    }
}
