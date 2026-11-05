using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class StatementAstTests
{
    [Test]
    public void Statement_StoresKindSpanAndEmptyTokens()
    {
        SqlTextSpan span = new SqlTextSpan(4, 11);
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Select, span);

        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(statement.IsKind(SqlStatementKind.Select), Is.True);
        Assert.That(statement.IsKind(SqlStatementKind.Insert), Is.False);
        Assert.That(statement.IsEmptyStatement(), Is.False);
        Assert.That(statement.Span, Is.SameAs(span));
        Assert.That(statement.HasTokens(), Is.False);
        Assert.That(statement.GetTokenCount(), Is.EqualTo(0));
        Assert.That(statement.Query, Is.Null);
        Assert.That(statement.Insert, Is.Null);
        Assert.That(statement.CreateTable, Is.Null);
        Assert.That(statement.Pragma, Is.Null);
        Assert.That(statement.Explain, Is.Null);
    }

    [Test]
    public void Statement_IdentifiesEmptyStatementKind()
    {
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Empty, new SqlTextSpan(3, 0));

        Assert.That(statement.IsEmptyStatement(), Is.True);
    }

    [Test]
    public void Statement_StoresTokenPayload()
    {
        SqlToken first = SqlToken.Create(SqlTokenKind.Keyword, new SqlTextSpan(0, 6), "SELECT");
        SqlToken second = SqlToken.Create(SqlTokenKind.NumericLiteral, new SqlTextSpan(7, 1), "1");
        SqlTextSpan span = new SqlTextSpan(0, 8);
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Select, span, new SqlToken[] { first, second });

        Assert.That(statement.HasTokens(), Is.True);
        Assert.That(statement.GetTokenCount(), Is.EqualTo(2));
        Assert.That(statement.GetTokenAt(0), Is.SameAs(first));
        Assert.That(statement.GetTokenAt(1), Is.SameAs(second));
    }

    [Test]
    public void Statement_AllowsTypedPayloadFields()
    {
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Insert, new SqlTextSpan(0, 12));
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(0, 0));
        SqlParseResult insertResult = SqlStatementParser.ParseScript("insert into items values (1)", SqlParseOptions.CreateSqlite());
        SqlParseResult tableResult = SqlStatementParser.ParseScript("create table items (id integer)", SqlParseOptions.CreateSqlite());
        SqlParseResult pragmaResult = SqlStatementParser.ParseScript("pragma cache_size", SqlParseOptions.CreateSqlite());

        statement.Query = query;
        statement.Insert = insertResult.Script.GetStatementAt(0).Insert;
        statement.CreateTable = tableResult.Script.GetStatementAt(0).CreateTable;
        statement.Pragma = pragmaResult.Script.GetStatementAt(0).Pragma;

        Assert.That(statement.Query, Is.SameAs(query));
        Assert.That(statement.Insert, Is.SameAs(insertResult.Script.GetStatementAt(0).Insert));
        Assert.That(statement.CreateTable, Is.SameAs(tableResult.Script.GetStatementAt(0).CreateTable));
        Assert.That(statement.Pragma, Is.SameAs(pragmaResult.Script.GetStatementAt(0).Pragma));
    }

    [Test]
    public void Statement_RejectsNullSpanAndTokens()
    {
        Assert.Throws<ArgumentNullException>(() => SqlStatement.Create(SqlStatementKind.Select, null!));
        Assert.Throws<ArgumentNullException>(() => SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 0), null!));
        Assert.Throws<ArgumentNullException>(() => SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 0), new SqlToken[] { null! }));
    }
}
