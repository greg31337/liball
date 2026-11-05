using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserComplexPrimaryTests
{
    [Test]
    public void ExpressionParser_ParsesParenthesizedExpression()
    {
        SqlExpression expression = Parse("(1 + 2)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Operand.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Span.Length, Is.EqualTo("(1 + 2)".Length));
    }

    [Test]
    public void ExpressionParser_ParsesRowValueExpression()
    {
        SqlExpression expression = Parse("(1, name, ?)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.RowValue));
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(3));
        Assert.That(expression.GetArgumentAt(0).Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.GetArgumentAt(1).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.GetArgumentAt(2).Kind, Is.EqualTo(SqlExpressionKind.Parameter));
    }

    [Test]
    public void ExpressionParser_ParsesCastExpression()
    {
        SqlExpression expression = Parse("cast(amount as decimal(10, 2))");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Cast));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.TypeName, Is.EqualTo("decimal(10, 2)"));
        Assert.That(expression.Span.Length, Is.EqualTo("cast(amount as decimal(10, 2))".Length));
    }

    [Test]
    public void ExpressionParser_ParsesSearchedCaseExpression()
    {
        SqlExpression expression = Parse("case when amount > 0 then 'positive' else 'zero' end");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Case));
        Assert.That(expression.Operand, Is.Null);
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(2));
        Assert.That(expression.GetArgumentAt(0).Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.GetArgumentAt(1).Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Alternate, Is.Not.Null);
        Assert.That(expression.Alternate!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [Test]
    public void ExpressionParser_ParsesSimpleCaseExpression()
    {
        SqlExpression expression = Parse("case status when 1 then 'one' when 2 then 'two' end");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Case));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(4));
        Assert.That(expression.Alternate, Is.Null);
    }

    [Test]
    public void ExpressionParser_ParsesSubqueryExpression()
    {
        SqlExpression expression = Parse("(select 1)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Subquery));
        Assert.That(expression.Query, Is.Not.Null);
        Assert.That(expression.Query!.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(expression.Query.Span.Length, Is.EqualTo("select 1".Length));
        Assert.That(expression.Span.Length, Is.EqualTo("(select 1)".Length));
    }

    [Test]
    public void ExpressionParser_ParsesExistsExpression()
    {
        SqlExpression expression = Parse("exists (select 1)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Exists));
        Assert.That(expression.Query, Is.Not.Null);
        Assert.That(expression.Query!.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(expression.Span.Length, Is.EqualTo("exists (select 1)".Length));
    }

    [Test]
    public void ExpressionParser_ParsesInSubqueryExpression()
    {
        SqlExpression expression = Parse("id in (select id from items)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(expression.Query, Is.Not.Null);
        Assert.That(expression.Arguments.Length, Is.EqualTo(0));
        Assert.That(expression.Query!.Span.Length, Is.EqualTo("select id from items".Length));
    }

    [Test]
    public void ExpressionParser_ComplexPrimaryExpressionsParticipateInBinaryExpressions()
    {
        SqlExpression expression = Parse("cast(amount as integer) + case when active then 1 else 0 end");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Cast));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Case));
    }

    [Test]
    public void ExpressionParser_ReportsDiagnosticsForMalformedComplexPrimaryExpressions()
    {
        SqlParserContext rowContext = Context("(1, 2");
        SqlExpressionParser rowParser = new SqlExpressionParser(rowContext);
        SqlParserContext castContext = Context("cast(amount integer)");
        SqlExpressionParser castParser = new SqlExpressionParser(castContext);
        SqlParserContext caseContext = Context("case when amount 1 end");
        SqlExpressionParser caseParser = new SqlExpressionParser(caseContext);
        SqlParserContext existsContext = Context("exists amount");
        SqlExpressionParser existsParser = new SqlExpressionParser(existsContext);

        SqlExpression row = rowParser.ParseExpression();
        SqlExpression cast = castParser.ParseExpression();
        SqlExpression caseExpression = caseParser.ParseExpression();
        SqlExpression exists = existsParser.ParseExpression();

        Assert.That(row.Kind, Is.EqualTo(SqlExpressionKind.RowValue));
        Assert.That(rowContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR013"));
        Assert.That(cast.Kind, Is.EqualTo(SqlExpressionKind.Cast));
        Assert.That(castContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR015"));
        Assert.That(caseExpression.Kind, Is.EqualTo(SqlExpressionKind.Case));
        Assert.That(caseContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR017"));
        Assert.That(exists.Kind, Is.EqualTo(SqlExpressionKind.Exists));
        Assert.That(existsContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR020"));
    }

    static SqlExpression Parse(string text)
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context(text));
        return parser.ParseExpression();
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
