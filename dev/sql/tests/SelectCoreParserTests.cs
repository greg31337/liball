using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SelectCoreParserTests
{
    [Test]
    public void StatementParser_AttachesSelectQueryWithSingleCore()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(statement.Query, Is.Not.Null);
        Assert.That(statement.Query!.GetSelectCoreCount(), Is.EqualTo(1));
        SqlSelectCore core = statement.Query.GetSelectCoreAt(0);
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(1));
        SqlResultColumn column = (SqlResultColumn)core.GetResultColumnAt(0);
        SqlExpression expression = column.Expression!;
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Literal, Is.Not.Null);
        Assert.That(expression.Literal!.SourceText, Is.EqualTo("1"));
    }

    [Test]
    public void QueryParser_ParsesDistinctAndMultipleResultExpressions()
    {
        SqlQuery query = ParseQuery("select distinct a, b + 1");

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(1));
        SqlSelectCore core = query.GetSelectCoreAt(0);
        Assert.That(core.IsDistinct, Is.True);
        Assert.That(core.IsAll, Is.False);
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(Column(core, 0).Expression!.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(Column(core, 1).Expression!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [Test]
    public void QueryParser_ParsesAllAndWildcardResult()
    {
        SqlQuery query = ParseQuery("select all *");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.IsAll, Is.True);
        Assert.That(core.IsDistinct, Is.False);
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(Column(core, 0).IsWildcard, Is.True);
        Assert.That(Column(core, 0).Expression!.Kind, Is.EqualTo(SqlExpressionKind.Wildcard));
    }

    [Test]
    public void QueryParser_StopsSelectCoreBeforeLaterClauses()
    {
        SqlQuery query = ParseQuery("select 1 from items where id = 1");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(core.HasFrom(), Is.True);
        Assert.That(core.GetFromAt(0).Name!.GetText(), Is.EqualTo("items"));
        Assert.That(core.HasWhere(), Is.True);
        Assert.That(core.Span.Length, Is.EqualTo("select 1 from items where id = 1".Length));
    }

    [Test]
    public void QueryParser_ParsesComplexExpressionResultColumn()
    {
        SqlQuery query = ParseQuery("select sum(amount) over (partition by category)");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.GetResultColumnCount(), Is.EqualTo(1));
        SqlExpression expression = Column(core, 0).Expression!;
        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.GetPartitionByCount(), Is.EqualTo(1));
    }

    [Test]
    public void QueryParser_ParsesExplicitAndImplicitResultColumnAliases()
    {
        SqlQuery query = ParseQuery("select a as alpha, b beta");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(Column(core, 0).HasAlias(), Is.True);
        Assert.That(Column(core, 0).Alias!.GetText(), Is.EqualTo("alpha"));
        Assert.That(Column(core, 1).HasAlias(), Is.True);
        Assert.That(Column(core, 1).Alias!.GetText(), Is.EqualTo("beta"));
    }

    [Test]
    public void QueryParser_ParsesQualifiedWildcardResultColumn()
    {
        SqlQuery query = ParseQuery("select items.*");
        SqlResultColumn column = Column(query.GetSelectCoreAt(0), 0);

        Assert.That(column.IsWildcard, Is.True);
        Assert.That(column.HasQualifier(), Is.True);
        Assert.That(column.Qualifier!.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void QueryParser_ReportsMissingAliasAfterAs()
    {
        SqlParserContext context = Context("select a as");
        SqlQueryParser parser = new SqlQueryParser(context);

        SqlQuery query = parser.ParseQuery();

        Assert.That(query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(context.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY003"));
    }

    [Test]
    public void QueryParser_ReportsMissingResultColumn()
    {
        SqlParserContext context = Context("select from items");
        SqlQueryParser parser = new SqlQueryParser(context);

        SqlQuery query = parser.ParseQuery();

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(0));
        Assert.That(context.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY002"));
    }

    [Test]
    public void QueryParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlQueryParser(null!));

        SqlQueryParser parser = new SqlQueryParser(Context("select 1"));
        Assert.Throws<ArgumentNullException>(() => parser.IsSelectCoreClauseStart(null!));
        Assert.Throws<ArgumentNullException>(() => parser.IsTableReferenceTerminator(null!));
    }

    static SqlResultColumn Column(SqlSelectCore core, int index)
    {
        return (SqlResultColumn)core.GetResultColumnAt(index);
    }

    static SqlQuery ParseQuery(string text)
    {
        SqlQueryParser parser = new SqlQueryParser(Context(text));
        return parser.ParseQuery();
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
