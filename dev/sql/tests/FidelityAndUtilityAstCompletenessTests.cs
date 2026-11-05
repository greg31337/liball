using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FidelityAndUtilityAstCompletenessTests
{
    [Test]
    public void TokenRange_SupportsEmptyMaximumAndOverflowBoundaries()
    {
        SqlTokenRange empty = new SqlTokenRange(5, 0);
        SqlTokenRange maximum = new SqlTokenRange(int.MaxValue, 0);

        Assert.That(empty.Start, Is.EqualTo(5));
        Assert.That(empty.Length, Is.EqualTo(0));
        Assert.That(empty.GetEnd(), Is.EqualTo(5));
        Assert.That(maximum.GetEnd(), Is.EqualTo(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTokenRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTokenRange(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTokenRange(int.MaxValue, 1));
    }

    [Test]
    public void UnsupportedSyntax_PreservesEveryFidelityRelationship()
    {
        SqlTextSpan span = new SqlTextSpan(0, 0);
        SqlToken token = new SqlToken(SqlTokenKind.BadToken, span, "");
        SqlUnsupportedSyntax marker = new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.DialectFeature, "unsupported feature", "", span, new SqlToken[] { token });

        Assert.That(marker.Kind, Is.EqualTo(SqlUnsupportedSyntaxKind.DialectFeature));
        Assert.That(marker.Description, Is.EqualTo("unsupported feature"));
        Assert.That(marker.SourceText, Is.Empty);
        Assert.That(marker.Span, Is.SameAs(span));
        Assert.That(marker.Tokens[0], Is.SameAs(token));
    }

    [Test]
    public void UnsupportedSyntax_RejectsEveryRequiredReferenceAndTokenItem()
    {
        SqlTextSpan span = new SqlTextSpan(0, 0);

        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, null!, "", span, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, "description", null!, span, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, "description", "", null!, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, "description", "", span, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, "description", "", span, new SqlToken[] { null! }));
    }

    [Test]
    public void RollbackTarget_PreservesTransactionPayloadAndRejectsNulls()
    {
        SqlName name = Name("work");
        SqlTextSpan span = new SqlTextSpan(0, 0);
        SqlRollbackTarget target = new SqlRollbackTarget(true, name, span);

        Assert.That(target.HasSavepointKeyword, Is.True);
        Assert.That(target.Name, Is.SameAs(name));
        Assert.That(target.Span, Is.SameAs(span));
        Assert.Throws<ArgumentNullException>(() => new SqlRollbackTarget(false, null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlRollbackTarget(false, name, null!));
    }

    [Test]
    public void UtilityNodes_AllowEveryOptionalRelationshipToBeAbsent()
    {
        SqlTextSpan span = new SqlTextSpan(0, 0);
        SqlPragmaStatement pragma = new SqlPragmaStatement(Name("pragma"), null, SqlPragmaValueKind.None, span);
        SqlAnalyzeStatement analyze = new SqlAnalyzeStatement(null, span);
        SqlVacuumStatement vacuum = new SqlVacuumStatement(null, null, span);
        SqlAttachStatement attach = new SqlAttachStatement(null, null, span);
        SqlDetachStatement detach = new SqlDetachStatement(null, span);
        SqlReindexStatement reindex = new SqlReindexStatement(null, span);
        SqlTransactionStatement transaction = new SqlTransactionStatement(SqlTransactionActionKind.Rollback, SqlTransactionMode.None, false, false, null, span);
        SqlSavepointStatement savepoint = new SqlSavepointStatement(null, span);
        SqlReleaseStatement release = new SqlReleaseStatement(false, null, span);
        SqlExplainStatement explain = new SqlExplainStatement(false, null, span);

        Assert.That(pragma.HasValue(), Is.False);
        Assert.That(analyze.HasTargetName(), Is.False);
        Assert.That(vacuum.HasDatabaseName(), Is.False);
        Assert.That(vacuum.HasIntoExpression(), Is.False);
        Assert.That(attach.HasDatabaseExpression(), Is.False);
        Assert.That(attach.HasSchemaName(), Is.False);
        Assert.That(detach.HasSchemaName(), Is.False);
        Assert.That(reindex.HasTargetName(), Is.False);
        Assert.That(transaction.HasSavepointName(), Is.False);
        Assert.That(savepoint.HasName(), Is.False);
        Assert.That(release.HasName(), Is.False);
        Assert.That(explain.HasStatement(), Is.False);
    }

    static SqlName Name(string text)
    {
        SqlTextSpan span = new SqlTextSpan(0, text.Length);
        return new SqlName(new SqlNamePart[] { new SqlNamePart(text, text, span, SqlNameQuoteStyle.None) }, span);
    }
}
