using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ReturningClauseAstTests
{
    [Test]
    public void ReturningClause_StoresResultColumns()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(10, 2));
        SqlResultColumn column = new SqlResultColumn(expression, false, null, null, new SqlTextSpan(10, 2), new SqlToken[0]);
        SqlTextSpan span = new SqlTextSpan(0, 12);
        SqlReturningClause returning = new SqlReturningClause(new SqlResultColumn[] { column }, span);

        Assert.That(returning.HasResultColumns(), Is.True);
        Assert.That(returning.GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(returning.GetResultColumnAt(0), Is.SameAs(column));
        Assert.That(returning.Span, Is.SameAs(span));
    }

    [Test]
    public void ReturningClause_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlReturningClause(null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlReturningClause(new SqlResultColumn[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlReturningClause(new SqlResultColumn[0], null!));
    }
}
