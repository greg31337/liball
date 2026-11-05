using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteParserRecoveryAndFidelityTests
{
    [TestCase("select 1 2", "SQLQUERY030")]
    [TestCase("values (1 2)", "SQLQUERY031")]
    [TestCase("select * from first a second b", "SQLQUERY032")]
    [TestCase("insert items values (1)", "SQLINSERT002")]
    [TestCase("insert into items values (1) on conflict(id) update set value = 1", "SQLINSERT012")]
    [TestCase("update items value = 1", "SQLUPDATE005")]
    [TestCase("delete items", "SQLDELETE002")]
    [TestCase("create table items (", "SQLDDL007")]
    [TestCase("alter table items rename old_name new_name", "SQLALTER007")]
    [TestCase("create index idx_items on items", "SQLINDEX009")]
    [TestCase("create trigger tr after insert items begin select 1; end", "SQLTRIGGER011")]
    [TestCase("attach 'archive.db' archive", "SQLATTACH003")]
    [TestCase("rollback to", "SQLTRANSACTION003")]
    [TestCase("savepoint", "SQLSAVEPOINT002")]
    [TestCase("release", "SQLRELEASE002")]
    [TestCase("explain query select 1", "SQLEXPLAIN002")]
    public void MalformedStatementGrammar_ReportsStableDiagnostic(string text, string expectedCode)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False, text);
        Assert.That(ContainsDiagnostic(result.Diagnostics, expectedCode), Is.True, text + " should report " + expectedCode);
    }

    [TestCase("value between 1", "SQLEXPR003")]
    [TestCase("value in (1, 2", "SQLEXPR004")]
    [TestCase("cast(1 integer)", "SQLEXPR015")]
    [TestCase("case when 1 2 end", "SQLEXPR017")]
    [TestCase("exists 1", "SQLEXPR020")]
    [TestCase("sum(1", "SQLEXPR006")]
    [TestCase("sum(1) filter where 1", "SQLEXPR010")]
    [TestCase("sum(1) over", "SQLEXPR022")]
    [TestCase("sum(1) over (partition value)", "SQLEXPR024")]
    [TestCase("sum(1) over (rows between 1 preceding 1 following)", "SQLEXPR025")]
    public void MalformedExpressionGrammar_ReportsStableDiagnostic(string text, string expectedCode)
    {
        SqlExpressionParseResult result = new SqlParserService().ParseExpression(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False, text);
        Assert.That(ContainsDiagnostic(result.Diagnostics, expectedCode), Is.True, text + " should report " + expectedCode);
    }

    [Test]
    public void RecoveryStepLimit_BoundsMissingCommaRecovery()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.MaxRecoverySteps = 2;

        SqlQueryParseResult result = new SqlParserService().ParseQuery("select 1 2 3 4 5", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(3));
        Assert.That(result.Diagnostics.GetCount(), Is.GreaterThanOrEqualTo(2));
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY030"));
        Assert.That(result.Diagnostics.GetAt(1).Code, Is.EqualTo("SQLQUERY030"));
    }

    [Test]
    public void StopOnFirstError_DisablesAdditionalRecovery()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.StopOnFirstError = true;

        SqlQueryParseResult result = new SqlParserService().ParseQuery("select 1 2 3", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY100"));
    }

    [Test]
    public void DiagnosticLimit_BoundsMalformedScriptDiagnostics()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.MaxDiagnostics = 2;

        SqlParseResult result = SqlStatementParser.ParseScript("insert items; update items; delete items", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetCount(), Is.EqualTo(2));
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
    }

    [Test]
    public void FragmentEntryPoints_PreserveSourceTokensSpansAndTrivia()
    {
        const string statementText = "/* lead */ select [value] from `items`; -- tail\n";
        const string queryText = "-- lead\nselect \"value\" /* result */ from items";
        const string expressionText = "/* lead */ :value + 0x10 -- tail\n";
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlParserService parser = new SqlParserService();

        SqlStatementParseResult statement = parser.ParseStatement(statementText, options);
        SqlQueryParseResult query = parser.ParseQuery(queryText, options);
        SqlExpressionParseResult expression = parser.ParseExpression(expressionText, options);

        Assert.That(statement.IsSuccess(), Is.True, DiagnosticText(statement.Diagnostics));
        Assert.That(query.IsSuccess(), Is.True, DiagnosticText(query.Diagnostics));
        Assert.That(expression.IsSuccess(), Is.True, DiagnosticText(expression.Diagnostics));
        Assert.That(statement.SourceText.Text, Is.EqualTo(statementText));
        Assert.That(query.SourceText.Text, Is.EqualTo(queryText));
        Assert.That(expression.SourceText.Text, Is.EqualTo(expressionText));
        Assert.That(statement.Statement.Span.Start, Is.EqualTo(statementText.IndexOf("select", StringComparison.Ordinal)));
        Assert.That(statement.Statement.Span.GetEnd(), Is.EqualTo(statementText.IndexOf(';') + 1));
        Assert.That(query.Query.Span.Start, Is.EqualTo(0));
        Assert.That(query.Query.Span.GetEnd(), Is.EqualTo(queryText.Length));
        Assert.That(expression.Expression.Span.Start, Is.EqualTo(expressionText.IndexOf(":value", StringComparison.Ordinal)));
        Assert.That(expression.Expression.Span.GetEnd(), Is.EqualTo(expressionText.IndexOf(" --", StringComparison.Ordinal)));
        AssertPreservedTokenStream(statement.Tokens, statementText);
        AssertPreservedTokenStream(query.Tokens, queryText);
        AssertPreservedTokenStream(expression.Tokens, expressionText);
        Assert.That(statement.Tokens[2].LeadingTrivia, Has.Some.Matches<SqlTrivia>(item => item.Kind == SqlTriviaKind.BlockComment));
        Assert.That(query.Tokens[2].LeadingTrivia, Has.Some.Matches<SqlTrivia>(item => item.Kind == SqlTriviaKind.LineComment));
        Assert.That(expression.Tokens[2].LeadingTrivia, Has.Some.Matches<SqlTrivia>(item => item.Kind == SqlTriviaKind.BlockComment));
    }

    [Test]
    public void MultilineMalformedInput_ReportsExactSourceLocation()
    {
        const string text = "select 1;\nselect value between 1;";

        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        SqlDiagnostic diagnostic = FindDiagnostic(result.Diagnostics, "SQLEXPR003");

        Assert.That(diagnostic.Span.Start, Is.EqualTo(text.IndexOf(';', "select 1;".Length)));
        Assert.That(diagnostic.GetLineNumber(), Is.EqualTo(2));
        Assert.That(diagnostic.GetColumnNumber(), Is.EqualTo("select value between 1".Length + 1));
        Assert.That(diagnostic.SourceText!.Text, Is.EqualTo(text));
    }

    static void AssertPreservedTokenStream(SqlToken[] tokens, string source)
    {
        Assert.That(tokens.Length, Is.GreaterThan(1));
        Assert.That(tokens[tokens.Length - 1].Kind, Is.EqualTo(SqlTokenKind.EndOfFile));
        int index = 0;
        string reconstructed = "";
        while (index < tokens.Length - 1)
        {
            reconstructed = reconstructed + tokens[index].Text;
            index = index + 1;
        }

        Assert.That(reconstructed, Is.EqualTo(source));
    }

    static bool ContainsDiagnostic(SqlDiagnosticBag diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.GetCount())
        {
            if (diagnostics.GetAt(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static SqlDiagnostic FindDiagnostic(SqlDiagnosticBag diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.GetCount())
        {
            if (diagnostics.GetAt(index).Code == code)
            {
                return diagnostics.GetAt(index);
            }

            index = index + 1;
        }

        Assert.Fail("Expected diagnostic " + code + DiagnosticText(diagnostics));
        return diagnostics.GetAt(0);
    }

    static string DiagnosticText(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.GetCount() == 0)
        {
            return "";
        }

        SqlDiagnostic diagnostic = diagnostics.GetAt(0);
        return ": " + diagnostic.Code + " " + diagnostic.Message;
    }
}
