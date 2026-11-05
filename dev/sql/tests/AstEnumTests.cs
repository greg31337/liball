using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstEnumTests
{
    [Test]
    public void StatementKind_ValuesAreStable()
    {
        Assert.That((int)SqlStatementKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlStatementKind.Empty, Is.EqualTo(1));
        Assert.That((int)SqlStatementKind.Select, Is.EqualTo(2));
        Assert.That((int)SqlStatementKind.Insert, Is.EqualTo(3));
        Assert.That((int)SqlStatementKind.CreateTable, Is.EqualTo(6));
        Assert.That((int)SqlStatementKind.Pragma, Is.EqualTo(16));
        Assert.That((int)SqlStatementKind.Analyze, Is.EqualTo(17));
        Assert.That((int)SqlStatementKind.Vacuum, Is.EqualTo(18));
        Assert.That((int)SqlStatementKind.Attach, Is.EqualTo(19));
        Assert.That((int)SqlStatementKind.Detach, Is.EqualTo(20));
        Assert.That((int)SqlStatementKind.Reindex, Is.EqualTo(21));
        Assert.That((int)SqlStatementKind.Begin, Is.EqualTo(22));
        Assert.That((int)SqlStatementKind.Commit, Is.EqualTo(23));
        Assert.That((int)SqlStatementKind.Rollback, Is.EqualTo(24));
        Assert.That((int)SqlStatementKind.End, Is.EqualTo(29));
        Assert.That((int)SqlStatementKind.Savepoint, Is.EqualTo(25));
        Assert.That((int)SqlStatementKind.Release, Is.EqualTo(26));
        Assert.That((int)SqlStatementKind.Explain, Is.EqualTo(27));
    }

    [Test]
    public void ExpressionKind_ValuesAreStable()
    {
        Assert.That((int)SqlExpressionKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlExpressionKind.Name, Is.EqualTo(1));
        Assert.That((int)SqlExpressionKind.Literal, Is.EqualTo(2));
        Assert.That((int)SqlExpressionKind.Binary, Is.EqualTo(6));
        Assert.That((int)SqlExpressionKind.FunctionCall, Is.EqualTo(7));
        Assert.That((int)SqlExpressionKind.Window, Is.EqualTo(19));
    }

    [Test]
    public void LiteralKind_ValuesAreStable()
    {
        Assert.That((int)SqlLiteralKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlLiteralKind.Null, Is.EqualTo(1));
        Assert.That((int)SqlLiteralKind.Integer, Is.EqualTo(2));
        Assert.That((int)SqlLiteralKind.String, Is.EqualTo(4));
        Assert.That((int)SqlLiteralKind.CurrentTimestamp, Is.EqualTo(9));
    }

    [Test]
    public void OperatorKind_ValuesAreStable()
    {
        Assert.That((int)SqlUnaryOperatorKind.None, Is.EqualTo(0));
        Assert.That((int)SqlUnaryOperatorKind.Negative, Is.EqualTo(2));
        Assert.That((int)SqlUnaryOperatorKind.Not, Is.EqualTo(4));
        Assert.That((int)SqlBinaryOperatorKind.None, Is.EqualTo(0));
        Assert.That((int)SqlBinaryOperatorKind.Add, Is.EqualTo(1));
        Assert.That((int)SqlBinaryOperatorKind.Concat, Is.EqualTo(6));
        Assert.That((int)SqlBinaryOperatorKind.NotEqual, Is.EqualTo(8));
        Assert.That((int)SqlBinaryOperatorKind.ShiftRight, Is.EqualTo(17));
        Assert.That((int)SqlBinaryOperatorKind.Or, Is.EqualTo(19));
    }

    [Test]
    public void JoinKind_ValuesAreStable()
    {
        Assert.That((int)SqlJoinKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlJoinKind.Comma, Is.EqualTo(1));
        Assert.That((int)SqlJoinKind.Cross, Is.EqualTo(2));
        Assert.That((int)SqlJoinKind.Left, Is.EqualTo(4));
        Assert.That((int)SqlJoinKind.Natural, Is.EqualTo(7));
    }

    [Test]
    public void ConflictActionKind_ValuesAreStable()
    {
        Assert.That((int)SqlConflictActionKind.None, Is.EqualTo(0));
        Assert.That((int)SqlConflictActionKind.Rollback, Is.EqualTo(1));
        Assert.That((int)SqlConflictActionKind.Abort, Is.EqualTo(2));
        Assert.That((int)SqlConflictActionKind.Ignore, Is.EqualTo(4));
        Assert.That((int)SqlConflictActionKind.Replace, Is.EqualTo(5));
    }

    [Test]
    public void InsertSourceKind_ValuesAreStable()
    {
        Assert.That((int)SqlInsertSourceKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlInsertSourceKind.Values, Is.EqualTo(1));
        Assert.That((int)SqlInsertSourceKind.Select, Is.EqualTo(2));
        Assert.That((int)SqlInsertSourceKind.DefaultValues, Is.EqualTo(3));
    }

    [Test]
    public void UpsertActionKind_ValuesAreStable()
    {
        Assert.That((int)SqlUpsertActionKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlUpsertActionKind.DoNothing, Is.EqualTo(1));
        Assert.That((int)SqlUpsertActionKind.DoUpdate, Is.EqualTo(2));
    }

    [Test]
    public void ConstraintKind_ValuesAreStable()
    {
        Assert.That((int)SqlConstraintKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlConstraintKind.PrimaryKey, Is.EqualTo(1));
        Assert.That((int)SqlConstraintKind.NotNull, Is.EqualTo(2));
        Assert.That((int)SqlConstraintKind.Check, Is.EqualTo(4));
        Assert.That((int)SqlConstraintKind.ForeignKey, Is.EqualTo(9));
    }
}
