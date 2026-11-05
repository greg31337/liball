using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class UtilityAstTests
{
    [Test]
    public void PragmaStatement_StoresNameValueKindAndSpan()
    {
        SqlName name = Name("cache_size", 7);
        SqlExpression value = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(20, 4));
        SqlTextSpan span = new SqlTextSpan(0, 24);
        SqlPragmaStatement pragma = new SqlPragmaStatement(name, value, SqlPragmaValueKind.Equals, span);

        Assert.That(pragma.Name, Is.SameAs(name));
        Assert.That(pragma.Value, Is.SameAs(value));
        Assert.That(pragma.ValueKind, Is.EqualTo(SqlPragmaValueKind.Equals));
        Assert.That(pragma.HasValue(), Is.True);
        Assert.That(pragma.Span, Is.SameAs(span));
    }

    [Test]
    public void AnalyzeStatement_StoresOptionalTargetAndSpan()
    {
        SqlName name = Name("items", 8);
        SqlTextSpan span = new SqlTextSpan(0, 13);
        SqlAnalyzeStatement analyze = new SqlAnalyzeStatement(name, span);

        Assert.That(analyze.TargetName, Is.SameAs(name));
        Assert.That(analyze.HasTargetName(), Is.True);
        Assert.That(analyze.Span, Is.SameAs(span));
    }

    [Test]
    public void VacuumStatement_StoresOptionalDatabaseIntoAndSpan()
    {
        SqlName databaseName = Name("main", 7);
        SqlExpression intoExpression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(17, 11));
        SqlTextSpan span = new SqlTextSpan(0, 28);
        SqlVacuumStatement vacuum = new SqlVacuumStatement(databaseName, intoExpression, span);

        Assert.That(vacuum.DatabaseName, Is.SameAs(databaseName));
        Assert.That(vacuum.IntoExpression, Is.SameAs(intoExpression));
        Assert.That(vacuum.HasDatabaseName(), Is.True);
        Assert.That(vacuum.HasIntoExpression(), Is.True);
        Assert.That(vacuum.Span, Is.SameAs(span));
    }

    [Test]
    public void AttachAndDetachStatements_StorePayloadsAndSpan()
    {
        SqlExpression databaseExpression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(7, 10));
        SqlName schemaName = Name("aux", 21);
        SqlTextSpan attachSpan = new SqlTextSpan(0, 24);
        SqlTextSpan detachSpan = new SqlTextSpan(0, 10);
        SqlAttachStatement attach = new SqlAttachStatement(databaseExpression, schemaName, attachSpan);
        SqlDetachStatement detach = new SqlDetachStatement(schemaName, detachSpan);

        Assert.That(attach.DatabaseExpression, Is.SameAs(databaseExpression));
        Assert.That(attach.SchemaName, Is.SameAs(schemaName));
        Assert.That(attach.HasDatabaseExpression(), Is.True);
        Assert.That(attach.HasSchemaName(), Is.True);
        Assert.That(attach.Span, Is.SameAs(attachSpan));
        Assert.That(detach.SchemaName, Is.SameAs(schemaName));
        Assert.That(detach.HasSchemaName(), Is.True);
        Assert.That(detach.Span, Is.SameAs(detachSpan));
    }

    [Test]
    public void ReindexStatement_StoresOptionalTargetAndSpan()
    {
        SqlName name = Name("idx_items_name", 8);
        SqlTextSpan span = new SqlTextSpan(0, 22);
        SqlReindexStatement reindex = new SqlReindexStatement(name, span);

        Assert.That(reindex.TargetName, Is.SameAs(name));
        Assert.That(reindex.HasTargetName(), Is.True);
        Assert.That(reindex.Span, Is.SameAs(span));
    }

    [Test]
    public void TransactionStatement_StoresActionModeFlagsSavepointAndSpan()
    {
        SqlName savepointName = Name("sp1", 13);
        SqlTextSpan span = new SqlTextSpan(0, 16);
        SqlTransactionStatement transaction = new SqlTransactionStatement(SqlTransactionActionKind.Rollback, SqlTransactionMode.Immediate, true, true, savepointName, span);

        Assert.That(transaction.ActionKind, Is.EqualTo(SqlTransactionActionKind.Rollback));
        Assert.That(transaction.Mode, Is.EqualTo(SqlTransactionMode.Immediate));
        Assert.That(transaction.HasTransactionKeyword, Is.True);
        Assert.That(transaction.IsToSavepoint, Is.True);
        Assert.That(transaction.SavepointName, Is.SameAs(savepointName));
        Assert.That(transaction.HasSavepointName(), Is.True);
        Assert.That(transaction.Span, Is.SameAs(span));
    }

    [Test]
    public void SavepointAndReleaseStatements_StoreNamesFlagsAndSpan()
    {
        SqlName name = Name("sp1", 10);
        SqlTextSpan savepointSpan = new SqlTextSpan(0, 13);
        SqlTextSpan releaseSpan = new SqlTextSpan(0, 21);
        SqlSavepointStatement savepoint = new SqlSavepointStatement(name, savepointSpan);
        SqlReleaseStatement release = new SqlReleaseStatement(true, name, releaseSpan);

        Assert.That(savepoint.Name, Is.SameAs(name));
        Assert.That(savepoint.HasName(), Is.True);
        Assert.That(savepoint.Span, Is.SameAs(savepointSpan));
        Assert.That(release.HasSavepointKeyword, Is.True);
        Assert.That(release.Name, Is.SameAs(name));
        Assert.That(release.HasName(), Is.True);
        Assert.That(release.Span, Is.SameAs(releaseSpan));
    }

    [Test]
    public void ExplainStatement_StoresQueryPlanFlagInnerStatementAndSpan()
    {
        SqlStatement inner = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(8, 8));
        SqlTextSpan span = new SqlTextSpan(0, 16);
        SqlExplainStatement explain = new SqlExplainStatement(true, inner, span);

        Assert.That(explain.IsQueryPlan, Is.True);
        Assert.That(explain.Statement, Is.SameAs(inner));
        Assert.That(explain.HasStatement(), Is.True);
        Assert.That(explain.Span, Is.SameAs(span));
    }

    [Test]
    public void UtilityAst_EnumValuesAreStable()
    {
        Assert.That((int)SqlPragmaValueKind.None, Is.EqualTo(0));
        Assert.That((int)SqlPragmaValueKind.Equals, Is.EqualTo(1));
        Assert.That((int)SqlPragmaValueKind.Parenthesized, Is.EqualTo(2));
        Assert.That((int)SqlTransactionActionKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlTransactionActionKind.Begin, Is.EqualTo(1));
        Assert.That((int)SqlTransactionActionKind.Commit, Is.EqualTo(2));
        Assert.That((int)SqlTransactionActionKind.End, Is.EqualTo(3));
        Assert.That((int)SqlTransactionActionKind.Rollback, Is.EqualTo(4));
        Assert.That((int)SqlTransactionMode.None, Is.EqualTo(0));
        Assert.That((int)SqlTransactionMode.Deferred, Is.EqualTo(1));
        Assert.That((int)SqlTransactionMode.Immediate, Is.EqualTo(2));
        Assert.That((int)SqlTransactionMode.Exclusive, Is.EqualTo(3));
    }

    [Test]
    public void UtilityAst_RejectsNullInputs()
    {
        SqlName name = Name("x", 0);

        Assert.Throws<ArgumentNullException>(() => new SqlPragmaStatement(null!, null, SqlPragmaValueKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlPragmaStatement(name, null, SqlPragmaValueKind.None, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlAnalyzeStatement(null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlVacuumStatement(null, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlAttachStatement(null, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDetachStatement(null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlReindexStatement(null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTransactionStatement(SqlTransactionActionKind.Begin, SqlTransactionMode.None, false, false, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlSavepointStatement(null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlReleaseStatement(false, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlExplainStatement(false, null, null!));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
