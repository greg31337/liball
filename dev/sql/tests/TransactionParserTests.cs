using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TransactionParserTests
{
    [Test]
    public void StatementParser_AttachesBeginTransactionPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("begin immediate transaction", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Begin));
        Assert.That(statement.Transaction, Is.Not.Null);
        SqlTransactionStatement transaction = statement.Transaction!;
        Assert.That(transaction.ActionKind, Is.EqualTo(SqlTransactionActionKind.Begin));
        Assert.That(transaction.Mode, Is.EqualTo(SqlTransactionMode.Immediate));
        Assert.That(transaction.HasTransactionKeyword, Is.True);
        Assert.That(transaction.IsToSavepoint, Is.False);
        Assert.That(transaction.HasSavepointName(), Is.False);
    }

    [Test]
    public void TransactionParser_ParsesCommitEndAndRollback()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("commit transaction; end; rollback transaction;", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        SqlTransactionStatement commit = result.Script.GetStatementAt(0).Transaction!;
        SqlTransactionStatement end = result.Script.GetStatementAt(1).Transaction!;
        SqlTransactionStatement rollback = result.Script.GetStatementAt(2).Transaction!;
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Commit));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.End));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Rollback));
        Assert.That(commit.ActionKind, Is.EqualTo(SqlTransactionActionKind.Commit));
        Assert.That(commit.HasTransactionKeyword, Is.True);
        Assert.That(end.ActionKind, Is.EqualTo(SqlTransactionActionKind.End));
        Assert.That(end.HasTransactionKeyword, Is.False);
        Assert.That(rollback.ActionKind, Is.EqualTo(SqlTransactionActionKind.Rollback));
        Assert.That(rollback.HasTransactionKeyword, Is.True);
    }

    [Test]
    public void TransactionParser_ParsesRollbackToSavepoint()
    {
        SqlTransactionStatement transaction = ParseTransaction("rollback transaction to savepoint sp1");

        Assert.That(transaction.ActionKind, Is.EqualTo(SqlTransactionActionKind.Rollback));
        Assert.That(transaction.HasTransactionKeyword, Is.True);
        Assert.That(transaction.IsToSavepoint, Is.True);
        Assert.That(transaction.SavepointName!.GetText(), Is.EqualTo("sp1"));
    }

    [Test]
    public void TransactionParser_ParsesBeginModes()
    {
        SqlTransactionStatement deferred = ParseTransaction("begin deferred");
        SqlTransactionStatement exclusive = ParseTransaction("begin exclusive transaction");

        Assert.That(deferred.Mode, Is.EqualTo(SqlTransactionMode.Deferred));
        Assert.That(deferred.HasTransactionKeyword, Is.False);
        Assert.That(exclusive.Mode, Is.EqualTo(SqlTransactionMode.Exclusive));
        Assert.That(exclusive.HasTransactionKeyword, Is.True);
    }

    [Test]
    public void TransactionParser_ReportsUnexpectedBeginToken()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("begin read only", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLTRANSACTION002"));
        Assert.That(result.Script.GetStatementAt(0).Transaction, Is.TypeOf<SqlTransactionStatement>());
    }

    [Test]
    public void TransactionParser_ReportsMissingRollbackSavepoint()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("rollback to", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLTRANSACTION003"));
        Assert.That(result.Script.GetStatementAt(0).Transaction, Is.TypeOf<SqlTransactionStatement>());
    }

    [Test]
    public void TransactionParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlTransactionParser(null!));
    }

    static SqlTransactionStatement ParseTransaction(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Transaction!;
    }
}
