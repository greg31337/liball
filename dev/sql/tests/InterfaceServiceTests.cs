using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class InterfaceServiceTests
{
    [Test]
    public void ParserService_ImplementsPublicParserContract()
    {
        ISqlParser parser = new SqlParserService();
        SqlParseOptions options = SqlParseOptions.CreateSqlite();

        SqlParseResult script = parser.ParseScript("select 1", options);
        SqlStatementParseResult statement = parser.ParseStatement("delete from items", options);
        SqlQueryParseResult query = parser.ParseQuery("select 1 union all values (2)", options);
        SqlExpressionParseResult expression = parser.ParseExpression("1 + 2", options);
        SqlTokenizeResult tokens = parser.Tokenize("select", options);

        Assert.That(script.IsSuccess(), Is.True);
        Assert.That(statement.Statement.Kind, Is.EqualTo(SqlStatementKind.Delete));
        Assert.That(query.Query.CompoundParts.Length, Is.EqualTo(1));
        Assert.That(expression.Expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(tokens.GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.Keyword));
    }

    [Test]
    public void FormatterService_UsesInjectedParserContract()
    {
        SqlParseResult result = SqlParser.ParseScript("select 1", SqlParseOptions.CreateSqlite());
        StubSqlParser parser = new StubSqlParser(result);
        ISqlFormatter formatter = SqlFormatterService.Create(parser);

        string formatted = formatter.Format("input owned by the stub", SqlFormatOptions.CreateDefault());

        Assert.That(formatted, Is.EqualTo("SELECT 1;"));
        Assert.That(parser.ParseScriptCallCount, Is.EqualTo(1));
        Assert.That(parser.LastText, Is.EqualTo("input owned by the stub"));
        Assert.That(parser.LastOptions.GetDialectKind(), Is.EqualTo(SqlDialectKind.SQLite));
    }

    [Test]
    public void FormatterService_RejectsNullParserDependency()
    {
        Assert.Throws<ArgumentNullException>(() => SqlFormatterService.Create(null!));
    }

    [Test]
    public void CustomDialectDescriptor_CanConfigureParsingThroughPublicApi()
    {
        ISqlDialect dialect = new TestDialect();
        SqlParseOptions options = SqlParseOptions.CreateForDialect(dialect);

        SqlTokenizeResult result = SqlParser.Tokenize("widget", options);

        Assert.That(dialect.GetKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(dialect.GetName(), Is.EqualTo("Test SQL"));
        Assert.That(result.GetTokenAt(0).Kind, Is.EqualTo(SqlTokenKind.Keyword));
        Assert.That(options.Dialect.GetCanonicalKeyword(result.GetTokenAt(0).Text), Is.EqualTo("WIDGET"));
    }

    class StubSqlParser : ISqlParser
    {
        public SqlParseResult Result;
        public int ParseScriptCallCount;
        public string LastText;
        public SqlParseOptions LastOptions;

        public StubSqlParser(SqlParseResult result)
        {
            Result = result;
            LastText = "";
            LastOptions = SqlParseOptions.CreateDefault();
        }

        public SqlParseResult ParseScript(string text, SqlParseOptions options)
        {
            ParseScriptCallCount = ParseScriptCallCount + 1;
            LastText = text;
            LastOptions = options;
            return Result;
        }

        public SqlStatementParseResult ParseStatement(string text, SqlParseOptions options)
        {
            throw new NotSupportedException();
        }

        public SqlQueryParseResult ParseQuery(string text, SqlParseOptions options)
        {
            throw new NotSupportedException();
        }

        public SqlExpressionParseResult ParseExpression(string text, SqlParseOptions options)
        {
            throw new NotSupportedException();
        }

        public SqlTokenizeResult Tokenize(string text, SqlParseOptions options)
        {
            throw new NotSupportedException();
        }
    }

    class TestDialect : ISqlDialect
    {
        public SqlDialectKind GetKind()
        {
            return SqlDialectKind.Generic;
        }

        public string GetName()
        {
            return "Test SQL";
        }

        public SqlDialectOptions CreateOptions()
        {
            SqlDialectOptions options = new SqlDialectOptions();
            options.CustomKeywords = new string[] { "WIDGET" };
            return options;
        }

        public ISqlDialectPolicy CreatePolicy()
        {
            return new SqlDialectPolicy(CreateOptions());
        }
    }
}
