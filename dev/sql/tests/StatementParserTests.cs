using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class StatementParserTests
{
    [Test]
    public void StatementParser_ParsesStatementListAndPreservesEmptyStatements()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select 1; ; insert into t values (1)", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Empty));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(result.Script.GetStatementAt(0).HasTokens(), Is.True);
        Assert.That(result.Script.GetStatementAt(1).GetTokenCount(), Is.EqualTo(1));
        Assert.That(result.Script.GetStatementAt(1).GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.Semicolon));
        Assert.That(result.Script.Span.Length, Is.EqualTo("select 1; ; insert into t values (1)".Length));
        Assert.That(result.HasSourceText(), Is.True);
    }

    [Test]
    public void StatementParser_IgnoresTriviaOutsideStatements()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("  -- comment\n  ");

        Assert.That(result.Script.IsEmpty(), Is.True);
        Assert.That(result.Script.Span.Length, Is.EqualTo("  -- comment\n  ".Length));
        Assert.That(result.HasTokens(), Is.True);
    }

    [Test]
    public void StatementParser_ClassifiesKnownStatementKinds()
    {
        SqlParseResult result = SqlStatementParser.ParseScript(
            "create temp table t(x); create unique index i on t(x); create virtual table v using fts5(x); drop trigger tr; pragma user_version; begin; end;",
            SqlParseOptions.CreateSqlite());

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(7));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.CreateTable));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.CreateIndex));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.CreateVirtualTable));
        Assert.That(result.Script.GetStatementAt(3).Kind, Is.EqualTo(SqlStatementKind.DropTrigger));
        Assert.That(result.Script.GetStatementAt(4).Kind, Is.EqualTo(SqlStatementKind.Pragma));
        Assert.That(result.Script.GetStatementAt(5).Kind, Is.EqualTo(SqlStatementKind.Begin));
        Assert.That(result.Script.GetStatementAt(6).Kind, Is.EqualTo(SqlStatementKind.End));
    }

    [Test]
    public void StatementParser_ReportsOptionsDiagnosticsButKeepsStatements()
    {
        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.AllowMultipleStatements = false;
        options.AllowEmptyStatements = false;
        SqlParseResult result = SqlStatementParser.ParseScript("select 1;;", options);

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(2));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Empty));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(result.GetDiagnosticAt(0).Code, Is.EqualTo("SQLPARSE002"));
        Assert.That(result.GetDiagnosticAt(1).Code, Is.EqualTo("SQLPARSE001"));
    }

    [Test]
    public void StatementParser_MergesLexerDiagnosticsIntoParseResult()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("'unterminated");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Unknown));
    }

    [Test]
    public void StatementParser_ParseStatementConsumesOneStatement()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("select 1; insert into t values (1)");
        SqlParserContext context = SqlParserContext.Create(tokens);
        SqlStatementParser parser = new SqlStatementParser(context);

        SqlStatement statement = parser.ParseStatement();

        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(context.Peek().Text, Is.EqualTo(" "));
    }

    [Test]
    public void StatementParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlStatementParser(null!));
        Assert.Throws<ArgumentNullException>(() => SqlStatementParser.ParseScript(null!));
        Assert.Throws<ArgumentNullException>(() => SqlStatementParser.ParseScript("select 1", null!));

        SqlParseOptions options = SqlParseOptions.CreateDefault();
        options.MaxDiagnostics = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlStatementParser.ParseScript("select 1", options));
    }
}
