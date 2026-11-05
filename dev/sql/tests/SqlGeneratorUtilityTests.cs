using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorUtilityTests
{
    [Test]
    public void SqlGenerator_GeneratesPragmaAnalyzeVacuumAndReindex()
    {
        string pragmaEquals = GenerateStatement("pragma main.cache_size = -2000");
        string pragmaCall = GenerateStatement("pragma table_info(items)");
        string analyze = GenerateStatement("analyze main.items");
        string vacuum = GenerateStatement("vacuum temp into 'backup.db'");
        string plainVacuum = GenerateStatement("vacuum");
        string reindex = GenerateStatement("reindex main.idx_items_name");
        string plainReindex = GenerateStatement("reindex");

        Assert.That(pragmaEquals, Is.EqualTo("PRAGMA main.cache_size = -2000"));
        Assert.That(pragmaCall, Is.EqualTo("PRAGMA table_info(items)"));
        Assert.That(analyze, Is.EqualTo("ANALYZE main.items"));
        Assert.That(vacuum, Is.EqualTo("VACUUM temp INTO 'backup.db'"));
        Assert.That(plainVacuum, Is.EqualTo("VACUUM"));
        Assert.That(reindex, Is.EqualTo("REINDEX main.idx_items_name"));
        Assert.That(plainReindex, Is.EqualTo("REINDEX"));
    }

    [Test]
    public void SqlGenerator_GeneratesAttachDetachTransactionsAndSavepoints()
    {
        string attach = GenerateStatement("attach database 'extra.db' as aux");
        string detach = GenerateStatement("detach aux");
        string begin = GenerateStatement("begin exclusive transaction");
        string commit = GenerateStatement("commit transaction");
        string end = GenerateStatement("end transaction");
        string rollback = GenerateStatement("rollback transaction");
        string rollbackTo = GenerateStatement("rollback to savepoint sp1");
        string savepoint = GenerateStatement("savepoint sp2");
        string release = GenerateStatement("release savepoint sp2");

        Assert.That(attach, Is.EqualTo("ATTACH DATABASE 'extra.db' AS aux"));
        Assert.That(detach, Is.EqualTo("DETACH aux"));
        Assert.That(begin, Is.EqualTo("BEGIN EXCLUSIVE TRANSACTION"));
        Assert.That(commit, Is.EqualTo("COMMIT TRANSACTION"));
        Assert.That(end, Is.EqualTo("END TRANSACTION"));
        Assert.That(rollback, Is.EqualTo("ROLLBACK TRANSACTION"));
        Assert.That(rollbackTo, Is.EqualTo("ROLLBACK TO SAVEPOINT sp1"));
        Assert.That(savepoint, Is.EqualTo("SAVEPOINT sp2"));
        Assert.That(release, Is.EqualTo("RELEASE SAVEPOINT sp2"));
    }

    [Test]
    public void SqlGenerator_GeneratesExplainStatements()
    {
        string queryPlan = GenerateStatement("explain query plan select id from items where id = ?");
        string insert = GenerateStatement("explain insert into items values (1)");

        Assert.That(queryPlan, Is.EqualTo("EXPLAIN QUERY PLAN SELECT id FROM items WHERE id = ?"));
        Assert.That(insert, Is.EqualTo("EXPLAIN INSERT INTO items VALUES (1)"));
    }

    [Test]
    public void SqlGenerator_UtilityApisRejectInvalidInputs()
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => generator.WritePragma(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteAnalyze(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteVacuum(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteAttach(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDetach(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteReindex(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteTransaction(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteSavepoint(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteRelease(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteExplain(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteStatement(SqlStatement.Create(SqlStatementKind.Pragma, new SqlTextSpan(0, 6))));
        Assert.Throws<ArgumentNullException>(() => generator.WriteStatement(SqlStatement.Create(SqlStatementKind.Analyze, new SqlTextSpan(0, 7))));
        Assert.Throws<ArgumentNullException>(() => generator.WriteStatement(SqlStatement.Create(SqlStatementKind.Explain, new SqlTextSpan(0, 7))));
    }

    static string GenerateStatement(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return SqlGenerator.GenerateStatement(result.Script.GetStatementAt(0));
    }
}
