using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ScriptAstTests
{
    [Test]
    public void Script_CreateEmptyCreatesEmptyRoot()
    {
        SqlScript script = SqlScript.CreateEmpty();

        Assert.That(script.IsEmpty(), Is.True);
        Assert.That(script.GetStatementCount(), Is.EqualTo(0));
        Assert.That(script.Span.Start, Is.EqualTo(0));
        Assert.That(script.Span.Length, Is.EqualTo(0));
    }

    [Test]
    public void Script_StoresStatementsAndSpan()
    {
        SqlStatement first = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 8));
        SqlStatement second = SqlStatement.Create(SqlStatementKind.Insert, new SqlTextSpan(9, 3));
        SqlTextSpan span = new SqlTextSpan(0, 12);
        SqlScript script = SqlScript.Create(new SqlStatement[] { first, second }, span);

        Assert.That(script.IsEmpty(), Is.False);
        Assert.That(script.GetStatementCount(), Is.EqualTo(2));
        Assert.That(script.GetStatementAt(0), Is.SameAs(first));
        Assert.That(script.GetStatementAt(1), Is.SameAs(second));
        Assert.That(script.Span, Is.SameAs(span));
    }

    [Test]
    public void Script_RejectsNullStatementsAndSpan()
    {
        Assert.Throws<ArgumentNullException>(() => SqlScript.Create(null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlScript.Create(new SqlStatement[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlScript.Create(new SqlStatement[0], null!));
    }
}
